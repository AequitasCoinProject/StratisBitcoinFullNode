﻿using NBitcoin;
using Stratis.Bitcoin.Features.SmartContracts;

namespace Stratis.SmartContracts.Networks
{
    public class SignedContractsPoARegTest : SmartContractsPoARegTest, ISignedCodePubKeyHolder
    {
        public Key SigningContractPrivKey { get;}

        public PubKey SigningContractPubKey { get;}

        public SignedContractsPoARegTest()
        {
            this.CoinName = "SignedContracts";
            this.NetworkName = "PoARegTest";

            this.SigningContractPrivKey = new Mnemonic("lava frown leave wedding virtual ghost sibling able mammal liar wide wisdom").DeriveExtKey().PrivateKey;
            this.SigningContractPubKey = this.SigningContractPrivKey.PubKey;
        }
    }
}
