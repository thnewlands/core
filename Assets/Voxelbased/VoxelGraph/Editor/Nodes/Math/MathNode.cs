using System;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;

namespace VoxelGraph.Editor.Nodes.Math
{
    [Serializable]
    public abstract class MathNode : NodeModel
    {
        public VoxelGraph VoxelGraph { get; set; }

        protected MathNode()
        {
        }
        
        protected float GetValue(IPortModel port)
        {
            if (port == null)
                return 0;
            var node = port.GetConnectedEdges().FirstOrDefault()?.FromPort.NodeModel;
            MathNode leftMathNode = node as MathNode;
            if (node is MathNode mathNode)
                return mathNode.Evaluate();
            else if (node is IVariableNodeModel varNode)
                return (float)varNode.VariableDeclarationModel.InitializationModel.ObjectValue;
            else if (node is IConstantNodeModel constNode)
                return (float)constNode.ObjectValue;
            else
                return (float)port.EmbeddedValue.ObjectValue;
        }

        public abstract float Evaluate();

        public abstract void ResetConnections();
    }
}