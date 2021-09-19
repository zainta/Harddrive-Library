// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;

namespace HDDL.Collections
{
	/// <summary>
	/// Provides Stack and List like functionality for a given type
	/// </summary>
	public class ListStack<T>
	{
		#region Declarations

		List<T> m_content;

		#endregion

		#region Properties

		/// <summary>
		/// Allows native list indexing
		/// </summary>
		/// <param name="index">The index of the item to retrieve</param>
		/// <returns>The item specified</returns>
		public T this[int index]
		{
			get
			{
				return m_content[index];
			}
		}

		/// <summary>
		/// Whether or not the stacklist is empty
		/// </summary>
		public bool Empty
		{
			get
			{
				return m_content.Count == 0;
			}
			set
			{
				if (value)
				{
					m_content.Clear();
				}
			}
		}

		/// <summary>
		/// The number of items in the stack list
		/// </summary>
		public int Count
		{
			get
			{
				return m_content.Count;
			}
		}

		#endregion

		/// <summary>
		/// Create an empty list stack
		/// </summary>
		public ListStack()
		{
			m_content = new List<T>();
		}

		/// <summary>
		/// Create a ListStack around an existing IEnumerable
		/// </summary>
		/// <param name="initial">The initial contents</param>
		public ListStack(IEnumerable<T> initial)
		{
			m_content = new List<T>(initial);
		}

		#region Methods

		#region Content Manipulation

		/// <summary>
		/// Empty the TokenStack
		/// </summary>
		public void Clear()
		{
			m_content.Clear();
		}

		/// <summary>
		/// Add one of the provided type
		/// </summary>
		/// <param name="item">The item to add</param>
		public void Add(T item)
		{
			m_content.Add(item);
		}

		/// <summary>
		/// Add the item at the end of the list
		/// </summary>
		/// <param name="item">The item to add</param>
		public void Push(T item)
		{
			if (m_content.Count > 0)
			{
				m_content.Insert(0, item);
			}
			else
			{
				m_content.Add(item);
			}
		}

		/// <summary>
		/// Look to see what is at the given index
		/// </summary>
		/// <param name="index">The index to look at</param>
		/// <returns>The item at the given index</returns>
		public T Peek(int index)
		{
			return m_content[index];
		}

		/// <summary>
		/// Look to see what is at the front of the list
		/// </summary>
		/// <returns>The item at the given index</returns>
		public T Peek()
		{
			return m_content[0];
		}

		/// <summary>
		/// Retrieve and remove the item at the given index
		/// </summary>
		/// <param name="index">The index of the item to remove</param>
		/// <returns>The item at the given index</returns>
		public T Grab(int index)
		{
			T item = m_content[index];
			m_content.RemoveAt(index);

			return item;
		}

		/// <summary>
		/// Retrieve and remove the first item
		/// </summary>
		/// <returns>The first item</returns>
		public T Pop()
		{
			return Grab(0);
		}

		/// <summary>
		/// Discards up to and returns the Nth item
		/// </summary>
		/// <returns>The Nth item</returns>
		public T PopTo(int offset = 0)
		{
			for (int i = 0; i < offset; i++)
			{
				Pop();
			}

			return Pop();
		}

		/// <summary>
		/// Devour the given number of items without returning them
		/// </summary>
		/// <param name="count">The number of charcters to devour</param>
		public void Eat(int count = 1)
		{
			for (int i = 0; i < count; i++)
			{
				Grab(0);
			}
		}

		#endregion

		/// <summary>
		/// Returns the actual list inside of the TokenStack
		/// </summary>
		/// <returns>The actual contained list</returns>
		public List<T> ToList()
		{
			return m_content;
		}

		/// <summary>
		/// Add the ienumerable's content
		/// </summary>
		/// <param name="content">What to add</param>
		public void AddRange(IEnumerable<T> content)
		{
			m_content.AddRange(content);
		}

		#endregion
	}
}
