﻿using System;
using Stratis.Bitcoin.Networks;

namespace Stratis.Bitcoin.IntegrationTests.Common.TestNetworks
{
    public class BitcoinRegTestOverrideCoinbaseMaturity : BitcoinRegTest
    {
        public BitcoinRegTestOverrideCoinbaseMaturity(int coinbaseMaturity) : base()
        {
            this.CoinName = "Bitcoin";
            this.NetworkName = Guid.NewGuid().ToString();
            this.Consensus.CoinbaseMaturity = coinbaseMaturity;
        }
    }
}
