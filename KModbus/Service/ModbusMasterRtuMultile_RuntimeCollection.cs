using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KModbus.Service
{
    public class ModbusMasterRtuMultile_RuntimeCollection: IList<ModbusMasterRtu_Runtime>, IEnumerable
    {
        private List<ModbusMasterRtu_Runtime> listService;

        public ModbusMasterRtuMultile_RuntimeCollection()
        {
            this.listService = new List<ModbusMasterRtu_Runtime>();
        }

        public ModbusMasterRtu_Runtime this[int index] { get => this.listService[index]; set => this.listService[index] = value; }

        public int Count
        {
            get
            {
                return this.listService.Count;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(ModbusMasterRtu_Runtime item)
        {
            lock (this.listService)
            {
                this.listService.Add(item);
            }
        }

        public void Clear()
        {
            lock (this.listService)
            {
                this.listService.Clear();
            }
        }

        public bool Contains(ModbusMasterRtu_Runtime item)
        {
            lock (this.listService)
            {
                return this.listService.Contains(item);
            }
        }

        public void CopyTo(ModbusMasterRtu_Runtime[] array, int arrayIndex)
        {
            lock (this.listService)
            {
                this.listService.CopyTo(array, arrayIndex);
            }
        }

        public IEnumerator<ModbusMasterRtu_Runtime> GetEnumerator()
        {
            return this.listService.GetEnumerator();
        }

        public int IndexOf(ModbusMasterRtu_Runtime item)
        {
            return this.listService.IndexOf(item);
            //lock (this.listService)
            //{
                
            //}
        }

        public void Insert(int index, ModbusMasterRtu_Runtime item)
        {
            lock (this.listService)
            {
                this.listService.Insert(index, item);
            }
        }

        public bool Remove(ModbusMasterRtu_Runtime item)
        {
            lock (this.listService)
            {
                return this.listService.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            lock (this.listService)
            {
                this.listService.RemoveAt(index);
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

    }
}
