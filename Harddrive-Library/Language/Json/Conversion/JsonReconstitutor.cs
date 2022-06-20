// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Language.Json.Reflection;
using HDDL.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Given a JsonBase derivation, converts the given class into its actual representation
    /// </summary>
    class JsonReconstitutor
    {
        /// <summary>
        /// Lookup table for property
        /// </summary>
        private static ExpiringCache<string, Type> _cache;

        /// <summary>
        /// The minimum number of items an array can have before it will automatically be multithreaded (only one array can be multithreaded)
        /// </summary>
        public int MinimumMultiThreadItemCount { get; private set; }

        /// <summary>
        /// Whether or not the reconstitutor will evaluate massive loads in parallel
        /// </summary>
        public bool CanDistributeWork { get; private set; }

        /// <summary>
        /// The maximum permissible threads to employ for evaluation
        /// </summary>
        public int MaximumThreads { get; private set; }

        /// <summary>
        /// Creates an instance
        /// </summary>
        /// <param name="enableDistribution">Whether or not to opportunistically multithread operations</param>
        /// <param name="maxThreads">The maximum permissible threads to employ for evaluation</param>
        /// <param name="minimumMultiThreadItemCount">The minimum number of items an array can have before it will automatically be multithreaded (only one array can be multithreaded)</param>
        public JsonReconstitutor(bool enableDistribution, int maxThreads = 4, int minimumMultiThreadItemCount = 50)
        {
            MinimumMultiThreadItemCount = minimumMultiThreadItemCount;
            CanDistributeWork = enableDistribution;
            MaximumThreads = maxThreads;

            // cache infinitely
            _cache = new ExpiringCache<string, Type>(-1);
        }

        /// <summary>
        /// Attempts to convert the given JsonBase into the target type
        /// </summary>
        /// <typeparam name="TargetType">The requested target type</typeparam>
        /// <param name="jb">The JsonBase instance to convert</param>
        /// <returns>An instance of type TargetType upon success, null otherwise</returns>
        public TargetType Convert<TargetType>(JsonBase jb)
        {
            TargetType result = default(TargetType);
            if (Evaluate(jb, typeof(TargetType)))
            {
                try
                {
                    result = (TargetType)jb.AsObject();
                }
                catch (Exception ex)
                {
                    throw new JsonConversionException("An error was encountered during the deserialization process.", ex);
                }
            }
            else
            {
                throw new JsonConversionException("Target type does not match json.", Array.Empty<LogItemBase>());
            }

            return result;
        }

        /// <summary>
        /// Performs a fully exploratory conversion to the most probably match
        /// </summary>
        /// <param name="jb">The JsonBase instance to convert</param>
        /// <returns>An instance of the determined type upon success, null otherwise</returns>
        public object Convert(JsonBase jb)
        {
            if (Evaluate(jb, null))
            {
                return Convert(jb);
            }
            else
            {
                throw new JsonConversionException("Target type does not match json.", Array.Empty<LogItemBase>());
            }
        }

        #region JsonBase Target Type Determination Methods

        /// <summary>
        /// Takes a JsonBase sub-type and evalutes it to determine what type it should be converted into
        /// </summary>
        /// <param name="jb">The instance to evaluate</param>
        /// <param name="type">The optional type to evaluate against.  If omitted, an organic survey will be executed to determine the best option available</param>
        /// <param name="runningInParallel">Whether or not this method call was done as a parallel operation</param>
        /// <returns>True upon success, false otherwise</returns>
        private bool Evaluate(JsonBase jb, Type type, bool runningInParallel = false)
        {
            bool result = false;
            if (jb is JsonArray ja)
            {
                result = EvaluateItem(ja, type, runningInParallel);
            }
            else if (jb is JsonBag jbg)
            {
                result = EvaluateItem(jbg, type, runningInParallel);
            }
            else if (jb is ValueTypeQuantity vtq)
            {
                result = EvaluateItem(vtq, type);
            }

            return result;
        }

        /// <summary>
        /// Takes a JsonBase sub-type and evalutes it to determine what type it should be converted into
        /// </summary>
        /// <param name="jb">The instance to evaluate</param>
        /// <param name="runningInParallel">Whether or not this method call was done as a parallel operation</param>
        /// <returns>True upon success, false otherwise</returns>
        private bool Evaluate(JsonBase jb, bool runningInParallel = false)
        {
            return Evaluate(jb, null, runningInParallel);
        }

        /// <summary>
        /// Takes a type and determines if it is a potential match for the derived type
        /// </summary>
        /// <param name="vtq">The ValueTypeQuantity to evaluate</param>
        /// <param name="type">The type to evaluate</param>
        /// <returns></returns>
        private bool EvaluateItem(ValueTypeQuantity vtq, Type type)
        {
            if (vtq.ConvertTarget != null) return true;

            var result = false;
            try
            {
                if (type == vtq.Kind)
                {
                    result = true;
                }
                else if (type.IsAssignableFrom(vtq.Kind))
                {
                    vtq.SetType(type);
                    result = true;
                }
                else if (type.IsAssignableTo(vtq.Kind))
                {
                    vtq.SetType(type);
                    result = true;
                }
                else
                {
                    System.Convert.ChangeType(vtq.Value, type);
                    if (vtq.Kind != type)
                    {
                        vtq.SetType(type);
                        result = true;
                    }
                }
            }
            catch
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Takes a JsonBag and determines if it is a potential match for the derived type
        /// </summary>
        /// <param name="jb">The JsonBag to evaluate</param>
        /// <param name="type">The type to evaluate</param>
        /// <param name="runningInParallel">Whether or not this method call was done as a parallel operation</param>
        /// <returns></returns>
        private bool EvaluateItem(JsonBag jb, Type type = null, bool runningInParallel = false)
        {
            if (jb.ConvertTarget != null) return true;

            var result = false;
            var key = jb.GetKeyString();
            Type target;

            // only determine the type of each record pattern once
            if (_cache.Has(key))
            {
                target = _cache[key];
            }
            else
            {
                // quick determination check:
                // take all of the properties on the JsonBag 
                // look for a class with those properties as its only valid property set
                // if there is a single class matching those requirements then that's our target

                // first get all properties of all types that are assignable to variables of the value of the type parameter
                var targetProps =
                        from pt in
                            type != null ?
                                TypeHelper.GetRelevantTypes(jb).Where(t => t.IsAssignableTo(type))
                                :
                                TypeHelper.GetRelevantTypes(jb)
                        from vp in TypeHelper.GetValidProperties(pt)
                        from propName in jb.Values.Keys.AsParallel()
                        where
                            vp.Name.Equals(propName, StringComparison.InvariantCultureIgnoreCase)
                        select vp;

                // get all of the types that have those properties
                var rootTypes = (from tp in targetProps
                                from at in TypeHelper.GetAllTypes()
                                    .Where(t => TypeHelper.GetValidProperties(t).Where(tvp => tvp == tp).Any())
                                select at).ToArray();


                // get the average type (the first type that can store all of a given set of types) of the rootTypes
                target = TypeHelper.GetAverageType(rootTypes);
            }

            // the class "object" doesn't have properties, and so the average should *never* be object.
            // otherwise, use the average type
            if (target != typeof(object))
            {
                jb.SetType(target);
                _cache[key] = target;

                // if we determine the type then determining its children is easy

                // to determine children more quickly, hand them the type they should be
                // generate type pairings by finding the property with the name of the expected property on this JsonBag
                // take that property's name, type, and the JsonBase derivation representing its contents
                // and create a tuple binding it all together
                var targetValueAssociations =
                    from pp in TypeHelper.GetValidProperties(target)
                    from propName in jb.Values.Keys.AsParallel()
                    where
                        pp.Name.Equals(propName, StringComparison.InvariantCultureIgnoreCase)
                    select new Tuple<string, Type, JsonBase>(pp.Name, pp.PropertyType, jb.Values[propName]);

                var childSuccesses = new ConcurrentBag<bool>();
                // this method is used by both threaded and synchronous approaches
                Action<Tuple<string, Type, JsonBase>> childEvaluationMethod = (tva) =>
                {
                    bool r = false;
                    if (tva.Item3 is JsonArray ja)
                    {
                        r = EvaluateItem(ja, tva.Item2, true);
                    }
                    else if (tva.Item3 is JsonBag jbg)
                    {
                        r = EvaluateItem(jbg, tva.Item2, true);
                    }
                    else if (tva.Item3 is ValueTypeQuantity vtq)
                    {
                        r = EvaluateItem(vtq, tva.Item2);
                    }

                    childSuccesses.Add(r);
                };

                // this is to see if we are multi threading, but the operation doesn't change
                // operation:
                // take the tuple and run evaluate item against the proper target based on the expected type and the provided JsonBase
                if (CanDistributeWork &&
                    !runningInParallel &&
                    jb.Values.Values.Count >= MinimumMultiThreadItemCount)
                {
                    var queue = new ThreadedQueue<Tuple<string, Type, JsonBase>>((tva) => childEvaluationMethod(tva), MaximumThreads);
                    queue.Start(targetValueAssociations);
                    queue.WaitAll();
                }
                else
                {
                    foreach (var tva in targetValueAssociations)
                    {
                        childEvaluationMethod(tva);
                    }
                }

                if (childSuccesses.All(cs => cs == true))
                {
                    result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// Takes a type and determines if it is a potential match for the derived type
        /// </summary>
        /// <param name="ja">The json array to evaluate</param>
        /// <param name="type">The type to evaluate</param>
        /// <param name="runningInParallel">Whether or not this method call was done as a parallel operation</param>
        /// <returns></returns>
        private bool EvaluateItem(JsonArray ja, Type type = null, bool runningInParallel = false)
        {
            if (ja.ConvertTarget != null) return true;

            var result = false;
            var key = ja.GetKeyString();

            // evaluate children first
            var childSuccesses = new ConcurrentBag<bool>();
            if (CanDistributeWork && 
                !runningInParallel && 
                ja.Values.Count >= MinimumMultiThreadItemCount)
            {
                var queue = new ThreadedQueue<JsonBase>((ji) => HandleEvaluation(ji, GetProperItemType(type), childSuccesses), MaximumThreads);
                queue.Start(ja.Values);
                queue.WaitAll();
                if (ja.Values.Last().ConvertTarget == null)
                {
                    HandleEvaluation(ja.Values.Last(), GetProperItemType(type), childSuccesses);
                }
            }
            else
            {
                foreach (var ji in ja.Values)
                {
                    var r = Evaluate(ji, GetProperItemType(type), runningInParallel);
                    childSuccesses.Add(r);
                    if (!r)
                    {
                        break;
                    }
                }
            }

            // only continue the type assessment if all children successfully assessed themselves
            if (!childSuccesses.Where(cs => !cs).Any())
            {
                if (_cache.Has(key))
                {
                    ja.SetType(_cache[key]);
                    result = true;
                }
                else
                {
                    if (type == null)
                    {
                        // get the common base type for all of this JsonArray's content
                        var contentTypes = (from jb in ja.Values select jb.ConvertTarget).ToArray();
                        Type averageContentType = contentTypes.Length == 0 ? typeof(object) : TypeHelper.GetAverageType(contentTypes);

                        var arrayType = Array.CreateInstance(averageContentType, 0).GetType();
                        ja.SetType(arrayType);
                        // never cache empty array keys
                        if (key != "<>")
                        {
                            _cache[key] = arrayType;
                        }
                        result = true;
                    }
                    else
                    {
                        var arrayItemType = GetProperItemType(type);

                        // get all items that are not assignable to the item type (invalid for this array)
                        var anyExceptions = 
                            (
                                from jb in ja.Values 
                                where !jb.ConvertTarget.IsAssignableTo(arrayItemType) 
                                select jb
                            ).Any();
                        if (!anyExceptions)
                        {
                            ja.SetType(type);
                            // never cache empty array keys
                            if (key != "<>")
                            {
                                _cache[key] = type;
                            }
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Properly retrieves the element type from an enumerable, be it generic or fixed
        /// </summary>
        /// <param name="type">The IEnumerable type to explore</param>
        /// <returns></returns>
        private Type GetProperItemType(Type type)
        {
            Type result = null;
            if (type.IsArray)
            {
                result = type.GetElementType();
            }
            else if (type.IsGenericType)
            {
                var i = type.GetInterfaces().Where(i =>
                        i.IsGenericType &&
                        i.GetGenericTypeDefinition() == typeof(IEnumerable<>)).SingleOrDefault();
                if (i != null)
                {
                    result = i.GetGenericArguments().Single();
                }
            }

            return result;
        }

        /// <summary>
        /// Takes a JsonBase sub-type and evalutes it to determine what type it should be converted into, 
        /// stores a true in Outcomes upon successful evaluation and false otherwise
        /// </summary>
        /// <param name="jb">The instance to evaluate</param>
        /// <param name="type">The optional type to evaluate against.  If omitted, an organic survey will be executed to determine the best option available</param>
        /// <param name="Outcomes">Contains a running list of results</param>
        /// <returns>True upon success, false otherwise</returns>
        private void HandleEvaluation(JsonBase jb, Type type, ConcurrentBag<bool> Outcomes)
        {
            bool result = false;
            if (jb is JsonArray ja)
            {
                result = EvaluateItem(ja, type, true);
            }
            else if (jb is JsonBag jbg)
            {
                result = EvaluateItem(jbg, type, true);
            }
            else if (jb is ValueTypeQuantity vtq)
            {
                result = EvaluateItem(vtq, type);
            }
            else
            {

            }

            Outcomes.Add(result);
        }

        #endregion
    }
}
