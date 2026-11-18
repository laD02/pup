using System;
using System.Collections.Generic;
using UnityEngine;

namespace SealCatapult.Progress
{
    public enum BlockMaterial { Snow, Ice, Stone }

    [Serializable]
    public struct BlockSpec
    {
        public BlockMaterial material;
        public Vector2 position;   // centre, relative to the fortress origin (ice shelf surface)
        public Vector2 size;
        public float rotation;
        public int group;          // > 0: shatters when a floor button of this group is pressed (ice gates)
        public int wedge;          // 0 = square block; 1 = triangle rising to the right, -1 = rising to the left (beam ends, column feet)
    }

    public enum ObjectKind { Bumper, Tnt, Button, Fish, Cage, Boss }

    /// <summary>Arena objects: bumpers, TNT crates, floor buttons, fish, the boss cage and the boss itself.</summary>
    [Serializable]
    public struct ObjectSpec
    {
        public ObjectKind kind;
        public Vector2 position;   // bumper/fish/cage: centre; button/boss: feet
        public int group;          // button: group it presses; boss: group that knocks it
        public int value;          // boss: lives
    }

    /// <summary>What the bear is doing until it is hit: standing guard (taunts), eating from a bowl, or asleep.</summary>
    public enum BearMood { Guard, Eat, Sleep, Dangle }   // Dangle: sits on a ledge edge, legs hanging (place it at the edge)

    public enum LevelKind { Normal, Bonus, Boss }

    /// <summary>How the pup hangs in the launch pit: swinging on the rope (pendulum), or bobbing up and down on a
    /// bungee straight above the catapult (report §10.4, the genre's second launcher).</summary>
    /// <summary>How a pup reaches the catapult: swinging on the rope, bobbing on a bungee, or jumping off the bridge with
    /// no rope at all (user 16/11: "3 kiểu: đu dây, dây lên xuống và hải cẩu nhảy xuống ko dây, tùy màn").</summary>
    public enum LaunchStyle { Swing, Bungee, Drop }

    [Serializable]
    public struct BearSpec
    {
        public Vector2 position;   // feet, relative to the fortress origin
        public bool helmet;        // ice-helmet bear: 2 HP
        public BearMood mood;
    }

    /// <summary>Block = rectangle; RampUpRight rises toward +x (solid bottom-right half); RampUpLeft is its mirror.</summary>
    /// <summary>IceShelf: a flat slab of ice with a snow cap that floats in the open air (the reference's right-edge perches).</summary>
    public enum TerrainShape { Block, RampUpRight, RampUpLeft, IceShelf }

    /// <summary>Solid cobble rock of the arena (plateaus, pillars, ramps): never breaks, never moves. Centre + size, relative to the arena origin.</summary>
    [Serializable]
    public struct TerrainSpec
    {
        public TerrainShape shape;
        public Vector2 position;
        public Vector2 size;
    }

    /// <summary>One level: how many pups, and the arena layout (rock terrain, fortress blocks, bears).</summary>
    [CreateAssetMenu(menuName = "Seal Catapult/Level", fileName = "Level_")]
    public class LevelData : ScriptableObject
    {
        [Header("Launch")]
        [Min(1)] public int pufferCount = 5;
        [Tooltip("Bears that must be knocked out to pass; 0 = every bear")]
        [Min(0)] public int requiredBears;
        public LevelKind kind = LevelKind.Normal;
        public LaunchStyle launcher = LaunchStyle.Swing;

        [Header("Cave under the arena (report §11): a gap in the floor drops into a hidden chamber")]
        [Tooltip("Chamber x range, relative to the arena origin; caveTo <= caveFrom = no cave")]
        public float caveFrom, caveTo;
        [Tooltip("The opening in the floor, inside the chamber's x range")]
        public float gapFrom, gapTo;
        public bool HasCave => caveTo > caveFrom && gapTo > gapFrom;
        [Tooltip("0 Frost Bay (sunset), 1 Glacier River (day), 2 Polar Night (ice cave): picks the arena palette")]
        [Range(0, 2)] public int zone;

        [Header("Fortress")]
        [Min(0)] public int threeStarScore = 1500;
        public List<TerrainSpec> terrain = new List<TerrainSpec>();
        public List<BlockSpec> blocks = new List<BlockSpec>();
        public List<BearSpec> bears = new List<BearSpec>();
        public List<ObjectSpec> objects = new List<ObjectSpec>();

        [Header("Tutorial")]
        public bool showTutorial;
    }
}
