﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NBitcoin;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.IntegrationTests.Common;
using Stratis.Bitcoin.IntegrationTests.Common.EnvironmentMockUpHelpers;
using Stratis.Bitcoin.Networks;
using Xunit;

namespace Stratis.Bitcoin.IntegrationTests
{
    public class NodeSyncTests
    {
        private readonly Network powNetwork;
        private readonly Network posNetwork;

        public NodeSyncTests()
        {
            this.powNetwork = new BitcoinRegTest();
            this.posNetwork = new StratisRegTest();
        }

        public class StratisRegTestMaxReorg : StratisRegTest
        {
            public StratisRegTestMaxReorg()
            {
                this.Consensus = new NBitcoin.Consensus(
                consensusFactory: base.Consensus.ConsensusFactory,
                consensusOptions: base.Consensus.Options,
                coinType: 105,
                hashGenesisBlock: base.GenesisHash,
                subsidyHalvingInterval: 210000,
                majorityEnforceBlockUpgrade: 750,
                majorityRejectBlockOutdated: 950,
                majorityWindow: 1000,
                buriedDeployments: base.Consensus.BuriedDeployments,
                bip9Deployments: base.Consensus.BIP9Deployments,
                bip34Hash: new uint256("0x000000000000024b89b42a942fe0d9fea3bb44ab7bd1b19115dd6a759c0808b8"),
                ruleChangeActivationThreshold: 1916, // 95% of 2016
                minerConfirmationWindow: 2016, // nPowTargetTimespan / nPowTargetSpacing
                maxReorgLength: 10,
                defaultAssumeValid: null, // turn off assumevalid for regtest.
                maxMoney: long.MaxValue,
                coinbaseMaturity: 10,
                premineHeight: 2,
                premineReward: Money.Coins(98000000),
                proofOfWorkReward: Money.Coins(4),
                powTargetTimespan: TimeSpan.FromSeconds(14 * 24 * 60 * 60), // two weeks
                powTargetSpacing: TimeSpan.FromSeconds(10 * 60),
                powAllowMinDifficultyBlocks: true,
                powNoRetargeting: true,
                powLimit: base.Consensus.PowLimit,
                minimumChainWork: null,
                isProofOfStake: true,
                lastPowBlock: 12500,
                proofOfStakeLimit: new BigInteger(uint256.Parse("00000fffffffffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeLimitV2: new BigInteger(uint256.Parse("000000000000ffffffffffffffffffffffffffffffffffffffffffffffffffff").ToBytes(false)),
                proofOfStakeReward: Money.COIN);

                this.CoinName = "Stratis";
                this.NetworkName = Guid.NewGuid().ToString();
            }
        }

        [Fact]
        public void Pow_NodesCanConnectToEachOthers()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode node1 = builder.CreateStratisPowNode(this.powNetwork).Start();
                CoreNode node2 = builder.CreateStratisPowNode(this.powNetwork).Start();

                Assert.Empty(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Empty(node2.FullNode.ConnectionManager.ConnectedPeers);

                TestHelper.Connect(node1, node2);
                Assert.Single(node1.FullNode.ConnectionManager.ConnectedPeers);
                Assert.Single(node2.FullNode.ConnectionManager.ConnectedPeers);

                var behavior = node1.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.OfType<IConnectionManagerBehavior>().FirstOrDefault();
                Assert.False(behavior.AttachedPeer.Inbound);
                Assert.True(behavior.OneTry);
                behavior = node2.FullNode.ConnectionManager.ConnectedPeers.First().Behaviors.OfType<IConnectionManagerBehavior>().FirstOrDefault();
                Assert.True(behavior.AttachedPeer.Inbound);
                Assert.False(behavior.OneTry);
            }
        }

        [Fact]
        public void Pow_CanStratisSyncFromCore()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode stratisNode = builder.CreateStratisPowNode(this.powNetwork).Start();
                CoreNode coreNode = builder.CreateBitcoinCoreNode().Start();

                Block tip = coreNode.FindBlock(10).Last();
                TestHelper.ConnectAndSync(stratisNode, coreNode);

                TestHelper.Disconnect(stratisNode, coreNode);

                coreNode.FindBlock(10).Last();
                TestHelper.ConnectAndSync(coreNode, stratisNode);
            }
        }

        [Fact]
        public void Pow_CanStratisSyncFromStratis()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode stratisNode = builder.CreateStratisPowNode(this.powNetwork).Start();
                CoreNode stratisNodeSync = builder.CreateStratisPowNode(this.powNetwork).Start();
                CoreNode coreCreateNode = builder.CreateBitcoinCoreNode().Start();

                // first seed a core node with blocks and sync them to a stratis node
                // and wait till the stratis node is fully synced
                Block tip = coreCreateNode.FindBlock(5).Last();
                TestHelper.ConnectAndSync(stratisNode, coreCreateNode);

                TestHelper.WaitLoop(() => stratisNode.FullNode.ConsensusManager().Tip.Block.GetHash() == tip.GetHash());

                // Add a new stratis node which will download
                // the blocks using the GetData payload
                TestHelper.ConnectAndSync(stratisNodeSync, stratisNode);
                TestHelper.WaitLoop(() => stratisNodeSync.FullNode.ConsensusManager().Tip.Block.GetHash() == tip.GetHash());
            }
        }

        [Fact]
        public void Pow_CanCoreSyncFromStratis()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                CoreNode stratisNode = builder.CreateStratisPowNode(this.powNetwork).Start();
                CoreNode coreNodeSync = builder.CreateBitcoinCoreNode().Start();
                CoreNode coreCreateNode = builder.CreateBitcoinCoreNode().Start();

                // first seed a core node with blocks and sync them to a stratis node
                // and wait till the stratis node is fully synced
                Block tip = coreCreateNode.FindBlock(5).Last();
                TestHelper.ConnectAndSync(stratisNode, coreCreateNode);
                TestHelper.WaitLoop(() => stratisNode.FullNode.ConsensusManager().Tip.Block.GetHash() == tip.GetHash());

                // add a new stratis node which will download
                // the blocks using the GetData payload
                TestHelper.ConnectAndSync(coreNodeSync, stratisNode);
            }
        }

        [Fact]
        public void Pos_Given_NodesAreSynced_When_ABigReorgHappens_Then_TheReorgIsIgnored()
        {
            using (NodeBuilder builder = NodeBuilder.Create(this))
            {
                var stratisRegTestMaxReorg = new StratisRegTestMaxReorg();

                CoreNode miner = builder.CreateStratisPosNode(stratisRegTestMaxReorg, nameof(miner)).WithDummyWallet().Start();
                CoreNode syncer = builder.CreateStratisPosNode(stratisRegTestMaxReorg, nameof(syncer)).Start();
                CoreNode reorg = builder.CreateStratisPosNode(stratisRegTestMaxReorg, nameof(reorg)).WithDummyWallet().Start();

                TestHelper.MineBlocks(miner, 1);

                // Sync miner with syncer and reorg
                TestHelper.ConnectAndSync(miner, reorg);
                TestHelper.ConnectAndSync(miner, syncer);

                // Create a reorg by mining on two different chains
                TestHelper.Disconnect(miner, reorg);
                TestHelper.Disconnect(miner, syncer);
                TestHelper.MineBlocks(miner, 11);
                TestHelper.MineBlocks(reorg, 12);

                // Make sure the nodes are actually on different chains.
                Assert.NotEqual(miner.FullNode.Chain.GetBlock(2).HashBlock, reorg.FullNode.Chain.GetBlock(2).HashBlock);

                TestHelper.ConnectAndSync(miner, syncer);

                // The hash before the reorg node is connected.
                uint256 hashBeforeReorg = miner.FullNode.Chain.Tip.HashBlock;

                // Connect the reorg chain
                TestHelper.ConnectNoCheck(miner, reorg);
                TestHelper.ConnectNoCheck(syncer, reorg);

                // Wait for the synced chain to get headers updated.
                TestHelper.WaitLoop(() => !TestHelper.IsNodeConnected(reorg));

                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(miner, syncer));
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(reorg, miner) == false);
                TestHelper.WaitLoop(() => TestHelper.AreNodesSynced(reorg, syncer) == false);

                // Check that a reorg did not happen.
                Assert.Equal(hashBeforeReorg, syncer.FullNode.Chain.Tip.HashBlock);
            }
        }

        /// <summary>
        /// This test simulates scenario from issue #862.
        /// <para>
        /// Connection scheme:
        /// Network - Node1 - MiningNode
        /// </para>
        /// </summary>
        [Fact]
        public void Pow_MiningNodeWithOneConnection_AlwaysSynced()
        {
            string testFolderPath = Path.Combine(this.GetType().Name, nameof(Pow_MiningNodeWithOneConnection_AlwaysSynced));

            using (NodeBuilder nodeBuilder = NodeBuilder.Create(testFolderPath))
            {
                CoreNode minerNode = nodeBuilder.CreateStratisPowNode(this.powNetwork).WithDummyWallet().Start();
                CoreNode connectorNode = nodeBuilder.CreateStratisPowNode(this.powNetwork).WithDummyWallet().Start();
                CoreNode firstNode = nodeBuilder.CreateStratisPowNode(this.powNetwork).WithDummyWallet().Start();
                CoreNode secondNode = nodeBuilder.CreateStratisPowNode(this.powNetwork).WithDummyWallet().Start();

                TestHelper.Connect(minerNode, connectorNode);
                TestHelper.Connect(connectorNode, firstNode);
                TestHelper.Connect(connectorNode, secondNode);
                TestHelper.Connect(firstNode, secondNode);

                List<CoreNode> nodes = new List<CoreNode> { minerNode, connectorNode, firstNode, secondNode };

                nodes.ForEach(n =>
                {
                    TestHelper.MineBlocks(n, 1);
                    TestHelper.WaitForNodeToSync(nodes.ToArray());
                });

                Assert.Equal(minerNode.FullNode.Chain.Height, nodes.Count);

                // Random node on network generates a block.
                TestHelper.MineBlocks(firstNode, 1);
                TestHelper.WaitForNodeToSync(firstNode, connectorNode, secondNode);

                // Miner mines the block.
                TestHelper.MineBlocks(minerNode, 1);
                TestHelper.WaitForNodeToSync(minerNode, connectorNode);

                TestHelper.MineBlocks(connectorNode, 1);

                TestHelper.WaitForNodeToSync(nodes.ToArray());
            }
        }
    }
}
