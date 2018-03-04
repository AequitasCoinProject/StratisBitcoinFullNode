﻿using System.Collections.Generic;
using System.Threading.Tasks;
using NBitcoin;
using Stratis.Bitcoin.Features.Consensus;
using Stratis.Bitcoin.Features.SmartContracts.Consensus.Rules;
using Stratis.SmartContracts;
using Xunit;
using Block = NBitcoin.Block;

namespace Stratis.Bitcoin.Features.SmartContracts.Tests.Consensus.Rules
{
    public class OpCreateZeroValueTest
    {
        public OpCreateZeroValueTest()
        {
            Block.BlockSignature = false;
            Transaction.TimeStamp = false;
        }

        [Fact]
        public async Task OpCreateZeroValueRule_SuccessAsync()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(Network.RegTest);
            OpCreateZeroValueRule rule = testContext.CreateRule<OpCreateZeroValueRule>();

            RuleContext context = new RuleContext(new BlockValidationContext(), Network.RegTest.Consensus, testContext.Chain.Tip);
            context.BlockValidationContext.Block = new Block();

            var gasPriceSatoshis = 20;
            var gasLimit = 4000000;
            var smartContractCreateFee = 0;

            SmartContractCarrier carrier = SmartContractCarrier.CreateContract(1, new byte[] { }, (ulong)gasPriceSatoshis, (Gas)gasLimit);
            byte[] serialized = carrier.Serialize();
            Script script = new Script(serialized);

            context.BlockValidationContext.Block.Transactions = new List<Transaction>
            {
                new Transaction
                {
                    Outputs =
                    {
                        new TxOut(smartContractCreateFee, script)
                    }
                }
            };

            await rule.RunAsync(context);
        }

        [Fact]
        public async Task OpCreateZeroValueRule_MultipleOutputs_SuccessAsync()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(Network.RegTest);
            OpCreateZeroValueRule rule = testContext.CreateRule<OpCreateZeroValueRule>();

            RuleContext context = new RuleContext(new BlockValidationContext(), Network.RegTest.Consensus, testContext.Chain.Tip);
            context.BlockValidationContext.Block = new Block();

            var gasPriceSatoshis = 20;
            var gasLimit = 4000000;
            var utxoValue = 20000;
            var fees = 10000;

            SmartContractCarrier carrier = SmartContractCarrier.CreateContract(1, new byte[] { }, (ulong)gasPriceSatoshis, (Gas)gasLimit);
            byte[] serialized = carrier.Serialize();
            Script script = new Script(serialized);

            Transaction funding = new Transaction
            {
                Outputs =
                {
                    new TxOut(utxoValue, new Script())
                }
            };

            var transactionBuilder = new TransactionBuilder();

            // Need to disable dust prevention, other adding our smart contract TX with zero value will be ignored
            transactionBuilder.DustPrevention = false;

            transactionBuilder.AddCoins(funding);
            transactionBuilder.SendFees(fees);
            transactionBuilder.Send(script, 0);

            // Add a change output to the transaction
            transactionBuilder.SetChange(new Script());

            Transaction tx = transactionBuilder.BuildTransaction(false);

            context.BlockValidationContext.Block.Transactions = new List<Transaction>
            {
               tx
            };

            await rule.RunAsync(context);
        }

        [Fact]
        public async Task OpCreateZeroValueRule_FailureAsync()
        {
            TestRulesContext testContext = TestRulesContextFactory.CreateAsync(Network.RegTest);
            OpCreateZeroValueRule rule = testContext.CreateRule<OpCreateZeroValueRule>();

            RuleContext context = new RuleContext(new BlockValidationContext(), Network.RegTest.Consensus, testContext.Chain.Tip);
            context.BlockValidationContext.Block = new Block();

            var gasPriceSatoshis = 20;
            var gasLimit = 4000000;
            var smartContractCreateFee = 1;

            SmartContractCarrier carrier = SmartContractCarrier.CreateContract(1, new byte[] { }, (ulong)gasPriceSatoshis, (Gas)gasLimit);
            byte[] serialized = carrier.Serialize();
            Script script = new Script(serialized);

            context.BlockValidationContext.Block.Transactions = new List<Transaction>
            {
                new Transaction
                {
                    Outputs =
                    {
                        new TxOut(smartContractCreateFee, script)
                    }
                }
            };

            await Assert.ThrowsAsync<ConsensusErrorException>(async () => await rule.RunAsync(context));
        }
    }
}
