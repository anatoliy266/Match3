using UnityEngine;
using System.Collections;
using System;

namespace Assets.Game.Components
{
	[Serializable]
	public class TileSpawnChance 
	{
		public TileType Type;
		public float SpawnChance;
		public float currentChance;
	}
}