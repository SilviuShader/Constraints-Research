using System.Collections.Generic;
using System.Linq;

namespace LevelsWFC
{
    class SequenceComparer<T> : IEqualityComparer<IEnumerable<T>>
    {
        public bool Equals(IEnumerable<T> seq1, IEnumerable<T> seq2) =>
            seq1 != null && seq2 != null && seq1.SequenceEqual(seq2);

        public int GetHashCode(IEnumerable<T> seq) => 
            seq.Aggregate(1234567, (current, elem) => unchecked(current * 37 + elem.GetHashCode()));
    }
}