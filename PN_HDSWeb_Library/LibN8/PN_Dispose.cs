using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PN_HDSWeb_Library.LibN8
{
    public class PN_Dispose : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Add(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
            _disposables.Clear();
        }
    }
}
