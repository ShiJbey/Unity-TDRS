using System;
using System.Collections.Generic;
using System.Linq;
using Codice.Client.BaseCommands.Update.Transformers;
using UnityEngine;
using UnityEngine.Tilemaps;


namespace TDRS
{
	/// <summary>
	/// This is the main entry point and overall manager for all information related
	/// to the Trait-Driven Relationship System.
	///
	/// <para>
	/// This MonoBehaviour is responsible for managing information about all the
	/// traits, preconditions, and effects.
	/// </para>
	///
	/// <para>
	/// This is a singleton class. Only one TDRSManager should be present in a scene.
	/// </para>
	/// </summary>
	public class TDRSManager : MonoBehaviour
	{
		#region Helper Classes
		[Serializable]
		public struct StatConfig
		{
			public string statName;
			public float baseValue;
			public float maxValue;
			public float minValue;
			public bool isDiscrete;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// All the nodes (characters, groups, concepts) in the social graph
		/// </summary>
		protected Dictionary<string, TDRSNode> _nodes = new Dictionary<string, TDRSNode>();

		[SerializeField]
		protected List<StatConfig> _entityStats;

		[SerializeField]
		protected List<StatConfig> _relationshipStats;

		/// <summary>
		/// A list of TextAssets assigned within the Unity inspector
		/// </summary>
		[SerializeField]
		protected List<TextAsset> _traitDefinitions = new List<TextAsset>();

		/// <summary>
		/// A list of precondition factories to load during wake
		/// </summary>
		[SerializeField]
		protected List<PreconditionFactorySO> _preconditionFactories = new List<PreconditionFactorySO>();

		/// <summary>
		/// A list of effect factories to load during wake
		/// </summary>
		[SerializeField]
		protected List<EffectFactorySO> _effectFactories = new List<EffectFactorySO>();

		#endregion

		#region Properties

		public static TDRSManager Instance { get; private set; }
		public TraitLibrary TraitLibrary { get; private set; }
		public EffectLibrary EffectLibrary { get; private set; }
		public PreconditionLibrary PreconditionLibrary { get; private set; }

		#endregion

		#region Unity Methods
		private void Awake()
		{
			// Ensure there is only one instance of this MonoBehavior active within the scene
			if (Instance != null && Instance != this)
			{
				Destroy(this);
			}
			else
			{
				Instance = this;
			}

			PreconditionLibrary = new PreconditionLibrary();
			EffectLibrary = new EffectLibrary();
			TraitLibrary = new TraitLibrary();

			LoadPreconditionFactories();
			LoadEffectFactories();
			LoadTraits();
		}
		#endregion

		#region Content Loading Methods
		private void LoadPreconditionFactories()
		{
			foreach (var factory in _preconditionFactories)
			{
				PreconditionLibrary.AddFactory(factory.preconditionType, factory);
			}
		}

		private void LoadEffectFactories()
		{
			foreach (var factory in _effectFactories)
			{
				EffectLibrary.AddFactory(factory.effectType, factory);
			}
		}

		private void LoadTraits()
		{
			foreach (var textAsset in _traitDefinitions)
			{
				TraitLibrary.LoadTraits(textAsset.text);
			}

			TraitLibrary.InstantiateTraits(this);
		}
		#endregion

		#region Methods
		/// <summary>
		/// Retrieves the social entity with the given ID or creates one
		/// if one is not found.
		/// </summary>
		/// <param name="entityID"></param>
		/// <returns></returns>
		public TDRSNode GetNode(string entityID)
		{
			if (_nodes.ContainsKey(entityID))
			{
				return _nodes[entityID];
			}

			var node = new TDRSNode(this, entityID);

			foreach (var entry in _entityStats)
			{
				node.Stats[entry.statName] = new StatSystem.Stat(
					entry.baseValue, entry.minValue, entry.maxValue, entry.isDiscrete
				);
			}

			_nodes[entityID] = node;

			return node;
		}

		/// <summary>
		/// Creates gets the relationship from the owner to target and creates a
		/// new relationship if one does not exist.
		///
		/// <para>
		/// This adds the necessary stats active social rules when creating new relationships
		/// </para>
		/// </summary>
		/// <param name="ownerID"></param>
		/// <param name="targetID"></param>
		/// <returns></returns>
		public TDRSRelationship GetRelationship(string ownerID, string targetID)
		{
			var owner = GetNode(ownerID);
			var target = GetNode(targetID);

			if (owner.OutgoingRelationships.ContainsKey(target))
			{
				return owner.OutgoingRelationships[target];
			}

			var relationship = new TDRSRelationship(
				this, $"{ownerID}->{targetID}", owner, target
			);

			owner.OutgoingRelationships[target] = relationship;
			target.IncomingRelationships[owner] = relationship;

			// Configure stats
			foreach (var entry in _relationshipStats)
			{
				relationship.Stats[entry.statName] = new StatSystem.Stat(
					entry.baseValue, entry.minValue, entry.maxValue, entry.isDiscrete
				);
			}

			// Apply outgoing social rules from the owner
			foreach (var rule in owner.SocialRules.Rules)
			{
				if (rule.IsOutgoing && rule.CheckPreconditions(relationship))
				{
					rule.OnAdd(relationship);
					relationship.SocialRules.AddSocialRule(rule);
				}
			}

			// Apply incoming social rules from the target
			foreach (var rule in target.SocialRules.Rules)
			{
				if (!rule.IsOutgoing && rule.CheckPreconditions(relationship))
				{
					rule.OnAdd(relationship);
					relationship.SocialRules.AddSocialRule(rule);
				}
			}

			return relationship;
		}

		/// <summary>
		/// Add a trait to an entity
		/// </summary>
		/// <param name="entityID"></param>
		/// <param name="traitID"></param>
		public void AddTraitToNode(string entityID, string traitID)
		{
			var node = GetNode(entityID);
			var trait = TraitLibrary.GetTrait(traitID);
			node.Traits.AddTrait(trait);
			node.OnTraitAdded(trait);
		}

		/// <summary>
		/// Remove a trait from a node
		/// </summary>
		/// <param name="entityID"></param>
		/// <param name="traitID"></param>
		public void RemoveTraitFromNode(string entityID, string traitID)
		{
			var node = GetNode(entityID);
			var trait = TraitLibrary.GetTrait(traitID);
			node.OnTraitRemoved(trait);
			node.Traits.RemoveTrait(trait);
		}

		/// <summary>
		/// Remove a social rule from a node
		/// </summary>
		/// <param name="entityID"></param>
		/// <param name="socialRule"></param>
		public void AddSocialRuleToNode(string entityID, SocialRule socialRule)
		{
			var node = GetNode(entityID);
			node.SocialRules.AddSocialRule(socialRule);
			node.OnSocialRuleAdded(socialRule);
		}

		/// <summary>
		/// Remove a social rule from a node
		/// </summary>
		/// <param name="entityID"></param>
		/// <param name="socialRule"></param>
		public void RemoveSocialRuleFromNode(string entityID, SocialRule socialRule)
		{
			var node = GetNode(entityID);
			node.OnSocialRuleRemoved(socialRule);
			node.SocialRules.RemoveSocialRule(socialRule);
		}

		/// <summary>
		/// Remove all social rules on a node from a given source
		/// </summary>
		/// <param name="entityID"></param>
		/// <param name="socialRule"></param>
		public void RemoveAllSocialRulesFromSource(string entityID, object source)
		{
			var node = GetNode(entityID);
			var socialRules = node.SocialRules.Rules.ToList();
			for (int i = socialRules.Count(); i >= 0; i--)
			{
				var rule = socialRules[i];
				if (rule.Source == source)
				{
					RemoveSocialRuleFromNode(entityID, rule);
				}
			}
		}

		/// <summary>
		/// Remove a trait from a relationship
		/// </summary>
		/// <param name="ownerID"></param>
		/// <param name="targetID"></param>
		/// <param name="traitID"></param>
		public void AddTraitToRelationship(string ownerID, string targetID, string traitID)
		{
			var relationship = GetRelationship(ownerID, targetID);
			var trait = TraitLibrary.GetTrait(traitID);
			relationship.Traits.AddTrait(trait);
			relationship.OnTraitAdded(trait);
		}


		/// <summary>
		/// Adds a trait to the relationship between two characters
		/// </summary>
		/// <param name="ownerID"></param>
		/// <param name="targetID"></param>
		/// <param name="traitID"></param>
		public void RemoveTraitFromRelationship(string ownerID, string targetID, string traitID)
		{
			var relationship = GetRelationship(ownerID, targetID);
			var trait = TraitLibrary.GetTrait(traitID);
			relationship.OnTraitRemoved(trait);
			relationship.Traits.RemoveTrait(trait);
		}
		#endregion
	}
}
