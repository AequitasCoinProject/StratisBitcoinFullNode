﻿using System;
using System.IO;
using System.Net.Http;
using Xunit;

namespace NBitcoin.Tests
{
    public class pow_tests
    {
        [Fact]
        [Trait("UnitTest", "UnitTest")]
        public void CanCalculatePowCorrectly()
        {
            ConcurrentChain chain = new ConcurrentChain(Network.BitcoinMain);
            EnsureDownloaded("MainChain.dat", "https://aois.blob.core.windows.net/public/MainChain.dat");
            chain.Load(File.ReadAllBytes("MainChain.dat"));
            foreach(ChainedHeader block in chain.EnumerateAfter(chain.Genesis))
            {
                Target thisWork = block.GetWorkRequired(Network.BitcoinMain);
                Target thisWork2 = block.Previous.GetNextWorkRequired(Network.BitcoinMain);
                Assert.Equal(thisWork, thisWork2);
                Assert.True(block.CheckProofOfWorkAndTarget(Network.BitcoinMain));
            }
        }

        private static void EnsureDownloaded(string file, string url)
        {
            if(File.Exists(file))
                return;
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(5);
            byte[] data = client.GetByteArrayAsync(url).GetAwaiter().GetResult();
            File.WriteAllBytes(file, data);
        }
    }
}
