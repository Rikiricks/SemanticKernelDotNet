using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SKRag
{
    public class PolicyDocument
    {
        [VectorStoreKey]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [VectorStoreData]
        public string Title { get; set; } = string.Empty;

        [VectorStoreData]
        public string Content { get; set; } = string.Empty;

        [VectorStoreVector(Dimensions: 1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> ContentEmbedding { get; set; }
    }
}
