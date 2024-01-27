using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace TDRS
{
	/// <summary>
	/// Represents a social connection between two SocialAgents. It manages stats, and traits
	/// associated with the relationship.
	/// </summary>
	public class SocialRelationship : MonoBehaviour
	{
		#region Fields

		[SerializeField]
		private SocialAgent m_owner;

		[SerializeField]
		private SocialAgent m_target;

		[SerializeField]
		private List<StatInitializer> m_baseStats;

		[SerializeField]
		private List<string> m_baseTraits;

		private RelationshipEdge m_relationshipEdge;

		#endregion

		#region Events

		/// <summary>
		/// Event invoked when a trait is added to the entity
		/// </summary>
		public TraitAddedEvent OnTraitAdded;

		/// <summary>
		/// Event invoked when a trait is removed from the entity
		/// </summary>
		public TraitRemovedEvent OnTraitRemoved;

		/// <summary>
		/// Event invoked when a stat value changes on the entity
		/// </summary>
		public StatChangeEvent OnStatChange;

		/// <summary>
		/// Event invoked when an entity is ticked;
		/// </summary>
		public TickEvent OnTick;

		#endregion

		#region Properties

		/// <summary>
		/// Reference to the owner of the relationship
		/// </summary>
		public SocialAgent Owner => m_owner;

		/// <summary>
		/// Reference to the target of the relationship
		/// </summary>
		public SocialAgent Target => m_target;

		public RelationshipEdge Edge => m_relationshipEdge;

		/// <summary>
		/// A reference to the manager that owns this entity
		/// </summary>
		public SocialEngine Engine { get; protected set; }

		/// <summary>
		/// Initial values for this entity's stats.
		/// </summary>
		public List<StatInitializer> BaseStats => m_baseStats;

		/// <summary>
		/// IDs of traits to add when initializing the entity.
		/// </summary>
		public List<string> BaseTraits => m_baseTraits;

		#endregion

		#region Unity Lifecycle Methods

		protected void Start()
		{
			Engine = FindObjectOfType<SocialEngine>();

			if (Engine == null)
			{
				Debug.LogError("Cannot find GameObject with SocialEngine component in scene.");
			}

			m_relationshipEdge = Engine.RegisterRelationship(this);

			// add event listeners
			m_relationshipEdge.OnTick += HandleOnTick;
			m_relationshipEdge.Traits.OnTraitAdded += HandleTraitAdded;
			m_relationshipEdge.Traits.OnTraitRemoved += HandleTraitRemoved;
			m_relationshipEdge.Stats.OnValueChanged += HandleStatChange;
		}

		#endregion

		#region Public Methods

		public void AddTrait(string traitID, int duration = -1)
		{
			m_relationshipEdge.AddTrait(traitID, duration);
		}

		public void RemoveTrait(string traitID)
		{
			m_relationshipEdge.RemoveTrait(traitID);
		}

		#endregion

		#region Private Methods

		private void HandleOnTick(object sender, EventArgs args)
		{
			OnTick?.Invoke();
		}

		private void HandleStatChange(object sender, (string, float) args)
		{
			OnStatChange?.Invoke(args.Item1, args.Item2);
		}

		private void HandleTraitAdded(object sender, string trait)
		{
			OnTraitAdded?.Invoke(trait);
		}

		private void HandleTraitRemoved(object sender, string trait)
		{
			OnTraitRemoved?.Invoke(trait);
		}

		#endregion

		#region Custom Event Classes

		/// <summary>
		/// Event dispatched when an entity is ticked
		/// </summary>
		[System.Serializable]
		public class TickEvent : UnityEvent { }

		/// <summary>
		/// Event dispatched when a trait is added to a social entity
		/// </summary>
		[System.Serializable]
		public class TraitAddedEvent : UnityEvent<string> { }

		/// <summary>
		/// Event dispatched when a trait is removed from a social entity
		/// </summary>
		[System.Serializable]
		public class TraitRemovedEvent : UnityEvent<string> { }

		/// <summary>
		/// Event dispatched when a social entity has a stat that is changed
		/// </summary>
		[System.Serializable]
		public class StatChangeEvent : UnityEvent<string, float> { }

		#endregion
	}

}
