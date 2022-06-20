// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Collections;
using HDDL.Language.Json.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace HDDL.Language.Json.Conversion
{
    /// <summary>
    /// Static class in charge of managing JsonBags
    /// </summary>
    static class JsonBagHandler
    {
        #region Generation

        /// <summary>
        /// Generates and returns the appropriate json container type (JsonArray or JsonBag)
        /// </summary>
        /// <param name="obj">The object to represent</param>
        /// <returns>A JsonBase derived type, or null</returns>
        public static JsonBase GetAppropriateJsonContainer(object obj)
        {
            if (obj is null)
            {
                return null;
            }
            else if (obj is IConvertible || obj is Guid)
            {
                return GetAsSingle(obj);
            }
            else if (obj is IEnumerable)
            {
                return GetAsArray((IEnumerable)obj);
            }
            else
            {
                return GetAsBag(obj);
            }
        }

        /// <summary>
        /// Generates a ValueTypeQuantity representation of a single value
        /// </summary>
        /// <param name="obj">The to represent</param>
        /// <returns></returns>
        private static ValueTypeQuantity GetAsSingle(object obj)
        {
            if (obj == null) return null;
            if (obj is ValueTypeQuantity) return (ValueTypeQuantity)obj;

            ValueTypeQuantity result = null;
            if (obj is IConvertible)
            {
                result = new ValueTypeQuantity(obj);
            }
            else if (obj is Guid)
            {
                result = new ValueTypeQuantity(obj);
            }

            result.SetType(obj.GetType());
            return result;
        }

        /// <summary>
        /// Generates a JsonArray representation of an IEnumerable implementation
        /// </summary>
        /// <param name="obj">The IEnumerable to represent</param>
        /// <returns></returns>
        private static JsonArray GetAsArray(IEnumerable obj)
        {
            if (obj == null) return null;
            if (obj is JsonArray) return (JsonArray)obj;

            var ja = new JsonArray();
            foreach (var o in  obj)
            {
                ja.Values.Add(GetAppropriateJsonContainer(o));
            }

            ja.SetType(obj.GetType());
            return ja;
        }

        /// <summary>
        /// Generates a JsonBag representation of an object
        /// </summary>
        /// <param name="obj">The object to represent</param>
        private static JsonBag GetAsBag(object obj)
        {
            if (obj == null) return null;

            var jb = new JsonBag();
            var ps = new ConcurrentDictionary<string, JsonBase>();
            var props = GetValidProperties(obj);
            Parallel.ForEach(props,
                (prop) =>
                {
                    // this won't fail since we're foreaching through the list of properties without duplicates
                    ps.TryAdd(prop.Name, GetAppropriateJsonContainer(prop.GetValue(obj)));
                });

            foreach (var item in ps)
            {
                jb.Values.Add(item.Key, item.Value);
            }

            jb.SetType(obj.GetType());
            return jb;
        }

        /// <summary>
        /// Returns the info objects for properties that are not forbidden with JsonIgnore attributes
        /// </summary>
        /// <param name="obj">The object to query</param>
        /// <returns></returns>
        private static PropertyInfo[] GetValidProperties(object obj)
        {
            return TypeHelper.GetValidProperties(obj.GetType());
        }

        #endregion

        #region Jsonification

        /// <summary>
        /// Converts the provided object into json
        /// </summary>
        /// <param name="obj">The jsonbase to convert</param>
        /// <param name="appendTypeProperty">Whether or not JSON should include the $type property</param>
        /// <returns>The resulting json string</returns>
        //internal static string GetJson(JsonBase obj, bool appendTypeProperty)
        //{
        //    if (obj is null)
        //    {
        //        return null;
        //    }
        //    else if (obj is IConvertible || obj is Guid)
        //    {
        //        return GetSingleJson(obj, appendTypeProperty);
        //    }
        //    else if (obj is IList)
        //    {
        //        return GetArrayJson((IList)obj, appendTypeProperty);
        //    }
        //    else
        //    {
        //        return GetBagJson(obj, appendTypeProperty);
        //    }
        //}

        ///// <summary>
        ///// Converts a single value into a json string
        ///// </summary>
        ///// <param name="o">The value to convert</param>
        ///// <param name="appendTypeProperty">Whether or not JSON should include the $type property</param>
        ///// <returns>Returns a json string for a single item array</returns>
        //private static string GetSingleJson(object o, bool appendTypeProperty)
        //{
        //    return GetArrayJson(new object[] { o }, appendTypeProperty);
        //}

        #endregion

        #region Rebagification

        /// <summary>
        /// Converts a json string back into its JsonBase derived equivalent
        /// </summary>
        /// <param name="json">The json string to convert</param>
        /// <param name="errors">Any issues encountered</param>
        /// <returns></returns>
        public static JsonBase GetIntermediate(string json, out LogItemBase[] errors)
        {
            JsonTokenizer jt = new JsonTokenizer();
            jt.Tokenize(json, true);
            JsonBase result = null;

            if (jt.Outcome.Count == 0)
            {
                var issues = new List<LogItemBase>();
                result = new JsonBag();

                while (!jt.Tokens.Empty)
                {
                    if (jt.Tokens.Peek().Type == JsonTokenTypes.CurlyOpen)
                    {
                        result = MakeBag(jt.Tokens, issues);
                    }
                    else if (jt.Tokens.Peek().Type == JsonTokenTypes.SquareOpen)
                    {
                        result = MakeArray(jt.Tokens, issues);
                    }
                    else if (jt.Tokens.Peek().Type == JsonTokenTypes.EndOfJSON)
                    {
                        jt.Tokens.Pop();
                    }

                    // if we, at any point, have any problems, we're done
                    if (issues.Count > 0)
                    {
                        break;
                    }
                }

                errors = issues.ToArray();
            }
            else
            {
                errors = jt.Outcome.ToArray();
            }

            return result;
        }

        /// <summary>
        /// Generates an array from the incoming json tokens
        /// </summary>
        /// <param name="tokens">The remaining tokens</param>
        /// <param name="issues">Any issues that are encountered will be placed here</param>
        private static JsonArray MakeArray(ListStack<JsonToken> tokens, List<LogItemBase> issues)
        {
            var result = new JsonArray();

            if (tokens.Peek().Type == JsonTokenTypes.SquareOpen)
            {
                tokens.Pop();

                while (tokens.Peek().Type == JsonTokenTypes.SquareOpen ||
                    tokens.Peek().Type == JsonTokenTypes.CurlyOpen ||
                    tokens.Peek().Type == JsonTokenTypes.TypeAnnotation)
                {
                    if (tokens.Peek().Type == JsonTokenTypes.CurlyOpen)
                    {
                        result.Values.Add(MakeBag(tokens, issues));
                    }
                    else if (tokens.Peek().Type == JsonTokenTypes.SquareOpen)
                    {
                        result.Values.Add(MakeArray(tokens, issues));
                    }
                    else if (tokens.Peek().Type == JsonTokenTypes.TypeAnnotation)
                    {
                        GetTypeAnnotation(tokens, result, issues);
                    }
                    else
                    {
                        var val = GetValue(tokens, issues);
                        if (val is JsonBase)
                        {
                            result.Values.Add((JsonBase)val);
                        }
                        else
                        {
                            result.Values.Add(new ValueTypeQuantity(val));
                        }
                    }

                    if (tokens.Peek().Type == JsonTokenTypes.Comma)
                    {
                        tokens.Pop();
                    }
                }

                if (tokens.Peek().Type == JsonTokenTypes.SquareClose)
                {
                    tokens.Pop();
                }
                else
                {
                    issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Array end ']' expected."));
                }
            }
            else
            {
                issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Array start '[' expected."));
            }

            return result;
        }

        /// <summary>
        /// Generates a json bag from the incoming json tokens
        /// </summary>
        /// <param name="tokens">The remaining tokens</param>
        /// <param name="issues">Any issues that are encountered will be placed here</param>
        /// <returns></returns>
        private static JsonBag MakeBag(ListStack<JsonToken> tokens, List<LogItemBase> issues)
        {
            var result = new JsonBag();
            if (tokens.Peek().Type == JsonTokenTypes.CurlyOpen)
            {
                tokens.Pop();

                // loop through and get all of the properties
                while (issues.Count == 0 &&
                    (tokens.Peek().Type == JsonTokenTypes.String ||
                    tokens.Peek().Type == JsonTokenTypes.TypeAnnotation))
                {
                    if (tokens.Peek().Type == JsonTokenTypes.String)
                    {
                        var prop = MakeProperty(tokens, issues);
                        if (issues.Count == 0)
                        {
                            Add(prop, result, issues);
                        }

                        if (tokens.Peek().Type == JsonTokenTypes.Comma)
                        {
                            tokens.Pop();
                        }
                    }
                    else if (tokens.Peek().Type == JsonTokenTypes.TypeAnnotation)
                    {
                        GetTypeAnnotation(tokens, result, issues);
                    }
                    else
                    {
                        issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Unknown property definition."));
                    }
                }

                if (tokens.Peek().Type == JsonTokenTypes.CurlyClose)
                {
                    tokens.Pop();
                }
                else
                {
                    issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Object end '}' expected."));
                }
            }
            else
            {
                issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Object start '{' expected."));
            }

            return result;
        }

        /// <summary>
        /// Generates a json property bag from the incoming json tokens
        /// </summary>
        /// <param name="tokens">The remaining tokens</param>
        /// <param name="issues">Any issues that are encountered will be placed here</param>
        /// <returns></returns>
        private static JsonPropertyBag MakeProperty(ListStack<JsonToken> tokens, List<LogItemBase> issues)
        {
            JsonPropertyBag prop = null;
            if (tokens.Count > 0)
            {
                if (tokens.Peek().Type == JsonTokenTypes.String ||
                    tokens.Peek().Type == JsonTokenTypes.TypeAnnotation)
                {
                    var propDeclaration = tokens.Pop();
                    if (tokens.Peek().Type == JsonTokenTypes.Colon)
                    {
                        tokens.Pop();

                        prop = new JsonPropertyBag(propDeclaration, GetValue(tokens, issues));
                        if (issues.Count > 0)
                        {
                            issues.Add(new LogItemBase(propDeclaration.Column, propDeclaration.Row, $"Failed to convert nested json array."));
                            prop = null;
                        }
                    }
                    else
                    {
                        issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Colon ':' expected."));
                    }
                }
                else
                {
                    issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, "Unexpected end of json."));
                }
            }

            return prop;
        }

        /// <summary>
        /// Generates and returns a property value type
        /// </summary>
        /// <param name="tokens">The remaining tokens</param>
        /// <param name="issues">Any issues that are encountered will be placed here</param>
        /// <returns></returns>
        private static object GetValue(ListStack<JsonToken> tokens, List<LogItemBase> issues)
        {
            object result = null;

            // now determine what kind of property it is
            if (tokens.Peek().Type == JsonTokenTypes.Boolean)
            {
                var str = tokens.Pop().Literal;
                if (bool.TryParse(str, out bool r))
                {
                    result = r;
                }
                else
                {
                    result = str;
                }
            }
            else if (tokens.Peek().Type == JsonTokenTypes.String)
            {
                // we have to determine what type of string this is (datetime, timespan, normal, etc)
                var str = tokens.Pop().Literal;
                DateTime dt;
                TimeSpan ts;
                Guid g;
                if (DateTime.TryParse(str, out dt))
                {
                    result = dt;
                }
                else if (TimeSpan.TryParse(str, out ts))
                {
                    result = ts;
                }
                else if (Guid.TryParse(str, out g))
                {
                    result = g;
                }
                else
                {
                    result = str;
                }
            }
            else if (tokens.Peek().Type == JsonTokenTypes.RealNumber)
            {
                result = double.Parse(tokens.Pop().Literal);
            }
            else if (tokens.Peek().Type == JsonTokenTypes.WholeNumber)
            {
                var numText = tokens.Pop().Literal;
                int numI;
                long numL;
                if (int.TryParse(numText, out numI))
                {
                    result = numI;
                }
                else if (long.TryParse(numText, out numL))
                {
                    result = numL;
                }
                else
                {
                    issues.Add(new LogItemBase(tokens.Peek().Column, tokens.Peek().Row, $"Failed to convert whole number '{numText}'."));
                }
            }
            else if (tokens.Peek().Type == JsonTokenTypes.Null)
            {
                tokens.Pop();
                result = null;
            }
            else if (tokens.Peek().Type == JsonTokenTypes.CurlyOpen)
            {
                var errorPoint = tokens.Peek();
                var jb = MakeBag(tokens, issues);
                if (issues.Count == 0)
                {
                    result = jb;
                }
                else
                {
                    issues.Add(new LogItemBase(errorPoint.Column, errorPoint.Row, $"Failed to convert nested json object."));
                }
            }
            else if (tokens.Peek().Type == JsonTokenTypes.SquareOpen)
            {
                var errorPoint = tokens.Peek();
                var ja = MakeArray(tokens, issues);
                if (issues.Count == 0)
                {
                    result = ja;
                }
                else
                {
                    issues.Add(new LogItemBase(errorPoint.Column, errorPoint.Row, $"Failed to convert nested json array."));
                }
            }

            return result;
        }

        /// <summary>
        /// Safely adds a property to a jsonbag without the possibility of duplicate exceptions
        /// </summary>
        /// <param name="propDefinition">The complete definition of the property</param>
        /// <param name="jb">The json bag</param>
        /// <param name="issues">Any issues will be stored here</param>
        private static void Add(JsonPropertyBag propDefinition, JsonBag jb, List<LogItemBase> issues)
        {
            if (jb.Values.ContainsKey(propDefinition.Name))
            {
                issues.Add(new LogItemBase(propDefinition.Column, propDefinition.Row, "Duplicate property defined in json."));
            }
            else
            {
                jb.Values.Add(propDefinition.Name, propDefinition.Content);
            }
        }

        /// <summary>
        /// Retrieves and stores a type annotation if one is defined next
        /// </summary>
        /// <param name="tokens">The tokens to process</param>
        /// <param name="jb">The json base derivation to modify</param>
        /// <param name="issues">Where to store any issues encountered</param>
        private static void GetTypeAnnotation(ListStack<JsonToken> tokens, JsonBase jb, List<LogItemBase> issues)
        {
            if (tokens.Peek().Type == JsonTokenTypes.TypeAnnotation)
            {
                var propDefStart = tokens.Peek();
                var prop = MakeProperty(tokens, issues);
                if (prop.Content is ValueTypeQuantity)
                {
                    try
                    {
                        var c = (ValueTypeQuantity)prop.Content;
                        var t = Type.GetType(c.Value.ToString());
                        jb.SetType(t);

                        if (tokens.Peek().Type == JsonTokenTypes.Comma)
                        {
                            tokens.Pop();
                        }
                    }
                    catch (Exception ex)
                    {
                        issues.Add(new LogItemBase(propDefStart.Column, propDefStart.Row, $"Exception thrown upon attempt to produce type indicated by $type property.  \n\n{ex}"));
                    }
                }
                else
                {
                    issues.Add(new LogItemBase(propDefStart.Column, propDefStart.Row, $"Improper use of $type property detected."));
                }
            }
        }

        #endregion
    }
}
