﻿using System.Collections.Generic;
using System.Threading;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace VoxelbasedCom
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class Chunk : MonoBehaviour
    {

        public int chunkSize;
        public int3 chunkPos;

        public SimulationType simType;

        private MeshCollider meshCollider;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Isosurface isosurface;
        private MeshBuilder meshBuilder;
        //Mesh was not ready on the same frame, so we need to wait more frames.
        bool waitingForMesh = false;
        //This is needed after mesh.SetIndexBuffer data to make the mesh visible, for some reason
        SubMeshDescriptor desc = new SubMeshDescriptor();

        private void OnEnable()
        {
            meshCollider = GetComponent<MeshCollider>();
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        /// <summary>
        /// Init chunk and start mesh generation
        /// </summary>
        /// <param name="isosurface"></param>
        /// <param name="chunkSize"></param>
        public void CreateChunk(Isosurface isosurface, int chunkSize)
        {
            this.isosurface = isosurface;
            meshBuilder = isosurface.GetMeshBuilder(transform.position, chunkSize);

            

            BuildChunk();
        }

        public void ModifyChunk(Shape shape, float3 pos, float radius, DensityOperationType operationType)
        {
            meshBuilder.ScheduleMeshJob(isosurface.ScheduleDensityModification(shape, pos, radius, operationType, chunkSize, chunkPos));
            waitingForMesh = true;
        }

        private void Update()
        {
            if (waitingForMesh)
            {
                if (meshBuilder.GetMeshData(out MeshData meshData))
                {
                    GenerateMesh(meshData);
                }
            }
            else if (simType != SimulationType.None)
            {
                meshBuilder.ScheduleMeshJob(true);
                waitingForMesh = true;
            }
        }

        public void BuildChunk()
        {
            //chunkState = ChunkState.Waiting;
            if (meshBuilder.GetMeshData(out MeshData meshData))
            {
                Debug.Log("Mesh was ready on the same frame it was requested. Cool.");
                GenerateMesh(meshData);
            }
            else
                waitingForMesh = true;
        }

        /// <summary>
        /// Generate mesh from mesh data
        /// </summary>
        /// <param name="data">Contains data to build mesh</param>
        public void GenerateMesh(MeshData data)
        {
            waitingForMesh = false;

            Mesh mesh = meshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh();
            }

            mesh.Clear();

            mesh.bounds = new Bounds(new float3(chunkSize / 2), new float3(chunkSize));

            var vertexCount = data.counter.Count * 3;
            if (vertexCount == 0)
            {
                return;
            }

            var layout = new[]
                {
                    new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                };

            mesh.SetVertexBufferParams(vertexCount, layout);
            mesh.SetVertexBufferData(data.vertices, 0, 0, vertexCount, 0, MeshUpdateFlags.DontValidateIndices);

            mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt32);
            mesh.SetIndexBufferData(data.triangles, 0, 0, vertexCount, MeshUpdateFlags.DontValidateIndices);

            desc.indexCount = vertexCount;
            mesh.SetSubMesh(0, desc, MeshUpdateFlags.DontValidateIndices);
            
            mesh.uv = new Vector2[vertexCount];
            mesh.SetNormals(data.normals);

            meshFilter.mesh = mesh;

            if(GetComponent<MeshCollider>() != null)
            {
                GetComponent<MeshCollider>().sharedMesh = mesh;
            }

            if (gameObject != null)
            gameObject.SetActive(true);
            //Profiler.EndSample();
            //EditorApplication.isPlaying = false;
        }
        private void OnApplicationQuit()
        {
            meshBuilder.Dispose();
            isosurface.Dispose();
        }
    }
}