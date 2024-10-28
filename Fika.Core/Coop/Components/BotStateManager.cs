using Fika.Core.Coop.GameMode;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	public class BotStateManager : MonoBehaviour
	{
		public delegate void UpdateAction();
		public event UpdateAction OnUpdate;

		private int fixedUpdateCount;
		private int fixedUpdatesPerTick;

		public static BotStateManager Create(CoopGame game, FikaServer server)
		{
			BotStateManager component = game.gameObject.AddComponent<BotStateManager>();
			component.fixedUpdateCount = 0;
			component.fixedUpdatesPerTick = Mathf.FloorToInt(60f / server.SendRate);
			return component;
		}

		private void FixedUpdate()
		{
			fixedUpdateCount++;
			if (fixedUpdateCount >= fixedUpdatesPerTick)
			{
				OnUpdate?.Invoke();
				fixedUpdateCount = 0;
			}
		}

		private void OnDestroy()
		{
			OnUpdate = null;
		}
	}
}
