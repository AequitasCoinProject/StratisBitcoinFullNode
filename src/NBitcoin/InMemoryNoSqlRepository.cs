﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NBitcoin
{
    public class InMemoryNoSqlRepository : NoSqlRepository
    {
        private Dictionary<string, byte[]> table = new Dictionary<string, byte[]>();

        public InMemoryNoSqlRepository(Network network = null)
            :base(network ?? Network.BitcoinMain)
        {
        }

        protected override Task PutBytesBatch(IEnumerable<Tuple<string, byte[]>> enumerable)
        {
            foreach(Tuple<string, byte[]> data in enumerable)
            {
                if(data.Item2 == null)
                {
                    this.table.Remove(data.Item1);
                }
                else
                    this.table.AddOrReplace(data.Item1, data.Item2);
            }
            return Task.FromResult(true);
        }

        protected override Task<byte[]> GetBytes(string key)
        {
            byte[] result = null;
            this.table.TryGetValue(key, out result);
            return Task.FromResult(result);
        }
    }
}
