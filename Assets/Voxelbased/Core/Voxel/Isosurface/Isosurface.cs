﻿using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelbasedCom
{
    public class Isosurface : IDisposable
    {
        public ShapeSelector shapeSelector;
        public IsosurfaceAlgorithm isosurfaceAlgorithm;
        private Dictionary<Vector3, BaseModification> modifiers;

        public JobHandle densityHandle;
        public NativeArray<float> densityField;

        DensityProperties baseDensity;

        /// <summary>
        /// Create a new Isosurface instance
        /// </summary>
        /// <param name="index">The iteration index</param>
        public Isosurface(ShapeSelector shapeSelector, IsosurfaceAlgorithm algorithm, Dictionary<Vector3, BaseModification> modifiers, bool preGenerate, int chunkSize, int3 chunkPos, DensityProperties baseDensity)
        {
            this.shapeSelector = shapeSelector;
            this.isosurfaceAlgorithm = algorithm;
            this.modifiers = modifiers;
            this.baseDensity = baseDensity;

            if (preGenerate)
            {
                int chunkSizeWithBorder = chunkSize + baseDensity.borderSize;
                densityField = new NativeArray<float>(chunkSizeWithBorder * chunkSizeWithBorder * chunkSizeWithBorder, Allocator.Persistent);

                var densityJob = new DensityJob()
                {
                    chunkOffset = chunkPos,
                    chunkSize = chunkSizeWithBorder,
                    densityField = densityField,
                    shape = baseDensity.shape,
                    shapeCenter = baseDensity.centerPoint,
                    shapeRadius = baseDensity.shapeRadius,
                };
                densityHandle = densityJob.Schedule(chunkSizeWithBorder * chunkSizeWithBorder * chunkSizeWithBorder, 64);
            }
        }

        public JobHandle ScheduleDensityModification(Shape shape, float3 modificationCenter, float modificationRadius, OperationType operationType, int chunkSize, int3 chunkPos)
        {
            if (!densityField.IsCreated) return default;
            if (!densityHandle.IsCompleted) throw new Exception("There is a densityJob already running with the same handle!");

            int chunkSizeWithBorder = chunkSize + baseDensity.borderSize;

            var densityJob = new DensityJob()
            {
                chunkOffset = chunkPos,
                chunkSize = chunkSizeWithBorder,
                densityField = densityField,
                shape = shape,
                shapeCenter = modificationCenter,
                shapeRadius = modificationRadius,
                operationType = operationType
            };
            densityHandle = densityJob.Schedule(chunkSizeWithBorder * chunkSizeWithBorder * chunkSizeWithBorder, 64);
            return densityHandle;
        }

        public JobHandle ScheduleDensityUpdate(int chunkSize, int3 chunkPos)
        {
            if (!densityField.IsCreated) return default;

            int chunkSizeWithBorder = chunkSize + baseDensity.borderSize;

            var densityJob = new DensityJob()
            {
                chunkOffset = chunkPos,
                chunkSize = chunkSizeWithBorder,
                densityField = densityField,
                shape = baseDensity.shape,
                shapeCenter = baseDensity.centerPoint,
                shapeRadius = baseDensity.shapeRadius,
                time = Time.time * 2,
                simType = baseDensity.simulationType,
                operationType = OperationType.Set
            };
            densityHandle = densityJob.Schedule(chunkSizeWithBorder * chunkSizeWithBorder * chunkSizeWithBorder, 64);
            return densityHandle;
        }

        public virtual float GetDensity(float x, float y, float z, int chunkSize)
        {
            float baseDensity = densityField[CoordinateConversion.Flatten((int)x, (int)y, (int)z, chunkSize)];

            //Modify the density
            foreach (BaseModification modification in modifiers.Values)
            {
                Density shapeDensity = shapeSelector.GetShapeDensity(modification.shapeType, modification.position, modification.shapeSize);

                float modificationDensity = shapeDensity.GetDensity(x,y,z);

                switch (modification.operationType)
                {
                    case OperationType.Union:
                        baseDensity = Mathf.Min(baseDensity, modificationDensity);
                        break;
                    case OperationType.Difference:
                        baseDensity = Mathf.Max(baseDensity, -modificationDensity);
                        break;
                    case OperationType.Intersection:
                        baseDensity = Mathf.Max(baseDensity, modificationDensity);
                        break;
                }
            }
            return baseDensity;
        }

        public MeshBuilder GetMeshBuilder(Vector3 offset, int chunkSize)
        {
            switch (isosurfaceAlgorithm)
            {
                case IsosurfaceAlgorithm.Boxel:
                    return new Boxel.Boxel(this, offset, chunkSize);
                case IsosurfaceAlgorithm.MarchingCubes:
                    return new MarchingCubes.MarchingCubes(this, offset, chunkSize);
                case IsosurfaceAlgorithm.MarchingTetrahedra:
                    return new MarchingTetrahedra(this, offset, chunkSize);
                case IsosurfaceAlgorithm.NaiveSurfaceNets:
                    return new NaiveSurfaceNets(this, offset, chunkSize);
                case IsosurfaceAlgorithm.DualContouring:
                    return new DualContouringUniform(this, offset, chunkSize);
                case IsosurfaceAlgorithm.CubicalMarchingSquares:
                    return new CubicalMarchingSquares(this, offset, chunkSize);
                default:
                    throw new NotImplementedException();
            }
        }

        public void Dispose()
        {
            densityHandle.Complete();
            densityField.Dispose();
        }
    }

    /// <summary>
    /// Available isosurfaces algorithms
    /// </summary>
    public enum IsosurfaceAlgorithm
    {
        Boxel,
        MarchingCubes,
        MarchingTetrahedra,
        NaiveSurfaceNets,
        DualContouring,
        CubicalMarchingSquares
    }
    //Temp
    public class DensityProperties
    {
        public int borderSize;
        public Shape shape;
        public float3 centerPoint;
        public float shapeRadius;
        public SimulationType simulationType;
    }
}
