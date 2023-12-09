using System.Collections.Generic;
using UnityEngine;

namespace TDRS
{
	/// <summary>
	/// A vertex/node within the social graph. This might represent a character, faction, concept,
	/// or any entity that characters might have a relationship toward.
	/// </summary>
	public class TDRSNode : SocialEntity
	{
		#region Properties

		/// <summary>
		/// A GameObject associated with this entity
		/// </summary>
		public GameObject GameObject { get; set; }

		/// <summary>
		/// Relationships directed toward this entity
		/// </summary>
		public Dictionary<TDRSNode, TDRSRelationship> IncomingRelationships { get; }

		/// <summary>
		/// Relationships from this entity directed toward other entities
		/// </summary>
		public Dictionary<TDRSNode, TDRSRelationship> OutgoingRelationships { get; }

		#endregion

		#region Constructors

		public TDRSNode(
			TDRSManager manager,
			string entityID
		) : base(manager, entityID)
		{
			GameObject = null;
			IncomingRelationships = new Dictionary<TDRSNode, TDRSRelationship>();
			OutgoingRelationships = new Dictionary<TDRSNode, TDRSRelationship>();
		}

		public TDRSNode(
			TDRSManager manager,
			string entityID,
			GameObject gameObject
		) : base(manager, entityID)
		{
			GameObject = gameObject;
			IncomingRelationships = new Dictionary<TDRSNode, TDRSRelationship>();
			OutgoingRelationships = new Dictionary<TDRSNode, TDRSRelationship>();
		}

		#endregion

		#region Methods

		public override void OnTraitAdded(Trait trait)
		{
			if (GameObject != null)
			{
				var tdrsEntity = GameObject.GetComponent<TDRSEntity>();
				if (tdrsEntity != null)
				{
					tdrsEntity.OnTraitAdded.Invoke(trait);
				}
			}
		}

		public override void OnTraitRemoved(Trait trait)
		{
			if (GameObject != null)
			{
				var tdrsEntity = GameObject.GetComponent<TDRSEntity>();
				if (tdrsEntity != null)
				{
					tdrsEntity.OnTraitRemoved.Invoke(trait);
				}
			}
		}

		public override void OnSocialRuleAdded(SocialRule rule)
		{
			Dictionary<TDRSNode, TDRSRelationship> relationships;

			if (rule.IsOutgoing)
			{
				relationships = OutgoingRelationships;
			}
			else
			{
				relationships = IncomingRelationships;
			}

			foreach (var (_, relationship) in relationships)
			{
				if (rule.CheckPreconditions(relationship))
				{
					relationship.SocialRules.AddSocialRule(rule);
					rule.OnAdd(relationship);
				}
			}
		}

		public override void OnSocialRuleRemoved(SocialRule rule)
		{
			Dictionary<TDRSNode, TDRSRelationship> relationships;
			if (rule.IsOutgoing)
			{
				relationships = OutgoingRelationships;
			}
			else
			{
				relationships = IncomingRelationships;
			}

			foreach (var (_, relationship) in relationships)
			{
				if (relationship.SocialRules.HasSocialRule(rule))
				{
					rule.OnRemove(relationship);
					relationship.SocialRules.RemoveSocialRule(rule);
				}
			}
		}

		#endregion
	}
}
