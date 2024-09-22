using Fika.Core.Coop.GameMode;
using Fika.Core.Networking;
using UnityEngine;

namespace Fika.Core.Coop.Components
{
	public class BotStateManager : MonoBehaviour
	{
		public delegate void UpdateAction();
		public event UpdateAction OnUpdate;

		private float updateRate;
		private float frameCounter;

		public static BotStateManager Create(CoopGame game, FikaServer server)
		{
			BotStateManager component = game.gameObject.AddComponent<BotStateManager>();
			component.updateRate = server.SendRate;
			return component;
		}

		private void Update()
		{
			float dur = 1f / updateRate;
			frameCounter += Time.deltaTime;
			while (frameCounter >= dur)
			{
				frameCounter -= dur;
				OnUpdate?.Invoke();
			}
		}

		private void OnDestroy()
		{
			OnUpdate = null;
		}
	}
}
