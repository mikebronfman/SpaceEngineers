﻿using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Algorithms;
using VRageMath;

namespace Sandbox.Game.AI.Pathfinding
{
    public class MyPathfinding : MyPathFindingSystem<MyNavigationPrimitive>
    {
        private MyVoxelPathfinding m_voxelPathfinding;
        private MyGridPathfinding m_gridPathfinding;
        private MyNavmeshCoordinator m_navmeshCoordinator;

        public MyGridPathfinding GridPathfinding { get { return m_gridPathfinding; } }
        public MyVoxelPathfinding VoxelPathfinding { get { return m_voxelPathfinding; } }
        public MyNavmeshCoordinator Coordinator { get { return m_navmeshCoordinator; } }

        // Just a debug draw thing
        public long LastHighLevelTimestamp { get; set; }

        public readonly Func<long> NextTimestampFunction;
        private long GenerateNextTimestamp()
        {
            CalculateNextTimestamp();
            return GetCurrentTimestamp();
        }

        public MyPathfinding()
        {
            NextTimestampFunction = GenerateNextTimestamp;

            m_navmeshCoordinator = new MyNavmeshCoordinator();
            m_gridPathfinding = new MyGridPathfinding(m_navmeshCoordinator);
            m_voxelPathfinding = new MyVoxelPathfinding(m_navmeshCoordinator);
        }

        public void Update()
        {
            if (MyFakes.ENABLE_PATHFINDING)
            {
                m_gridPathfinding.Update();
                m_voxelPathfinding.Update();
            }
        }

        public void UnloadData()
        {
            m_gridPathfinding.UnloadData();
            m_voxelPathfinding.UnloadData();

            m_gridPathfinding = null;
            m_voxelPathfinding = null;
            m_navmeshCoordinator = null;
        }

        public MySmartPath FindPathGlobal(Vector3D begin, IMyDestinationShape end, MyEntity entity = null)
        {
            Debug.Assert(MyFakes.ENABLE_PATHFINDING, "Pathfinding is not enabled!");
            if (!MyFakes.ENABLE_PATHFINDING)
            {
                return null;
            }

            ProfilerShort.Begin("MyPathfinding.FindPathGlobal");

            // CH: TODO: Use pooling
            MySmartPath newPath = new MySmartPath(this);
            MySmartGoal newGoal = new MySmartGoal(end, entity);
            newPath.Init(begin, newGoal);

            ProfilerShort.End();
            return newPath;
        }

        public MyPath<MyNavigationPrimitive> FindPathLowlevel(Vector3D begin, Vector3D end)
        {
            MyPath<MyNavigationPrimitive> path = null;

            Debug.Assert(MyFakes.ENABLE_PATHFINDING, "Pathfinding is not enabled!");
            if (!MyFakes.ENABLE_PATHFINDING)
            {
                return path;
            }

            ProfilerShort.Begin("MyPathfinding.FindPathLowlevel");
            var startPrim = FindClosestPrimitive(begin, highLevel: false);
            var endPrim = FindClosestPrimitive(end, highLevel: false);
            if (startPrim != null && endPrim != null)
            {
                path = FindPath(startPrim, endPrim);
            }
            ProfilerShort.End();

            return path;
        }

        public MyNavigationPrimitive FindClosestPrimitive(Vector3D point, bool highLevel, MyEntity entity = null)
        {
            double closestDistSq = double.PositiveInfinity;
            MyNavigationPrimitive closestPrimitive = null;

            MyNavigationPrimitive closest = null;

            MyVoxelMap voxelMap = entity as MyVoxelMap;
            MyCubeGrid cubeGrid = entity as MyCubeGrid;

            if (voxelMap != null)
            {
                closestPrimitive = VoxelPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq, voxelMap);
            }
            else if (cubeGrid != null)
            {
                closestPrimitive = GridPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq, cubeGrid);
            }
            else
            {
                closest = VoxelPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                if (closest != null) closestPrimitive = closest;
                closest = GridPathfinding.FindClosestPrimitive(point, highLevel, ref closestDistSq);
                if (closest != null) closestPrimitive = closest;
            }

            return closestPrimitive;
        }

        public void DebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW == false) return;

            m_gridPathfinding.DebugDraw();
            m_voxelPathfinding.DebugDraw();

            if (MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES != VRage.Utils.MyWEMDebugDrawMode.NONE)
            {
                m_navmeshCoordinator.Links.DebugDraw(Color.Khaki);
            }
            if (MyFakes.DEBUG_DRAW_NAVMESH_HIERARCHY)
            {
                m_navmeshCoordinator.HighLevelLinks.DebugDraw(Color.LightGreen);
            }

            m_navmeshCoordinator.DebugDraw();
        }
    }
}
