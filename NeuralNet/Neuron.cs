using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeuralNet
{
    class Neuron
    {
        float threshold;
        NeuronList connections;
        WeightList weights;

        public Neuron(float threshold, NeuronList connections)
        {
            this.threshold = threshold;
            this.connections = connections;
            this.weights = new WeightList(connections.array.Length);
        }

        /// <summary>Adds a forward connection in a neural net with a weight.
        /// <para>NOTE: Connections should ONLY go forward. Maybe?</para>
        /// </summary>
        public void addConnection(Neuron n, float weight = 1.0f)
        {
            connections.add(n);
            weights.add(weight);
        }

        public void fire()
        {
            for(int i = 0; i < connections.array.Length; i++)
            {

            }
        }
    }
}
