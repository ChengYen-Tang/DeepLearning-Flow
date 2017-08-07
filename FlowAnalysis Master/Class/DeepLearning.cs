using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.Neuro;
using Accord.Neuro.Learning;
using Accord.Neuro.Networks;
using Accord.Math;

namespace FlowAnalysis.Class
{
    public class DeepLearning
    {
        private DeepBeliefNetwork DBNetwork;
        private DeepBeliefNetworkLearning Teacher;
        private BackPropagationLearning Teacher2;
        private double[][][] layerData;

        public DeepLearning(double LearningRate, string Path)
        {
            DBNetwork = DeepBeliefNetwork.Load(Path);
            Initialization(LearningRate);
        }

        public DeepLearning(double LearningRate, int inputsCount, params int[] hiddenNeurons)
        {
            DBNetwork = new DeepBeliefNetwork(inputsCount, hiddenNeurons);
            Initialization(LearningRate);
        }

        private void Initialization(double LearningRate)
        {
            new GaussianWeights(DBNetwork, 0.01).Randomize();
            DBNetwork.UpdateVisibleWeights();
            Teacher = new DeepBeliefNetworkLearning(DBNetwork)
            {
                Algorithm = (h, v, i) =>
                    new ContrastiveDivergenceLearning(h, v)
                    {
                        LearningRate = LearningRate,
                        Momentum = 0.5,
                        Decay = 0.001,
                    }
            };
            Teacher2 = new BackPropagationLearning(DBNetwork)
            {
                LearningRate = LearningRate,
                Momentum = 0.5
            };
        }

        //public int Learning(double[][] Inputs, double[][] Outputs)
        //{
        //    int BatchCount = Math.Max(1, Inputs.Length / 50);
        //    int[] Groups
        //        = Accord.Statistics.Classes.Random(Inputs.Length, BatchCount);
            
        //}
    }
}
