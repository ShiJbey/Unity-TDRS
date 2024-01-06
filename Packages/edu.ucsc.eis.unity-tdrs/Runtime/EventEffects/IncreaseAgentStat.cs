using UnityEngine;

namespace TDRS
{
	public class IncreaseAgentStatFactory : SocialEventEffectFactory
	{
		public class IncreaseAgentStat : ISocialEventEffect
		{
			protected SocialAgent m_agent;
			protected string m_statName;
			protected float m_value;
			protected int m_duration;
			protected string m_reason;

			public IncreaseAgentStat(
				SocialAgent agent,
				string statName,
				float value,
				int duration,
				string reason
			)
			{
				m_agent = agent;
				m_statName = statName;
				m_value = value;
				m_duration = duration;
				m_reason = reason;
			}

			public void Apply()
			{
				m_agent.Stats.AddModifier(
					new StatSystem.StatModifier(
						m_statName,
						m_reason,
						m_value,
						StatSystem.StatModifierType.FLAT,
						m_duration
					)
				);
			}
		}

		public override string EffectType => "IncreaseAgentStat";

		public override ISocialEventEffect CreateInstance(SocialEventContext ctx, params string[] args)
		{
			if (args.Length < 3)
			{
				throw new System.ArgumentException(
					"Incorrect number of arguments for IncreaseAgentStat. "
					+ $"Expected 3 but was {args.Length}."
				);
			}

			string agentVar = args[0];
			string statName = args[1];

			if (!float.TryParse(args[2], out var value))
			{
				throw new System.ArgumentException(
					$"Expected number as last argument but was '{args[2]}'"
				);
			}

			int duration = -1;

			if (args.Length >= 4)
			{
				if (!int.TryParse(args[3], out duration))
				{
					throw new System.ArgumentException(
						$"Expected integer as 4th argument but was '{args[3]}'"
					);
				}
			}

			return new IncreaseAgentStat(
				ctx.Engine.GetAgent(
					ctx.Bindings[agentVar]
				),
				statName,
				value,
				duration,
				ctx.Description
			);
		}
	}
}
