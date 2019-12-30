using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UGUIDots.Transforms.Systems {

    public struct ResolutionChangeEvt : IComponentData {
        public byte Value;
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ConsumeChangeEvtSystem : JobComponentSystem {

        private struct ConsumeJob : IJobForEachWithEntity<ResolutionChangeEvt> {

            public EntityCommandBuffer.Concurrent CmdBuffer;

            public void Execute(Entity entity, int index, ref ResolutionChangeEvt c0) {
                CmdBuffer.DestroyEntity(index, entity);
            }
        }

        private EntityCommandBufferSystem cmdBufferSystem;

        protected override void OnCreate() {
            cmdBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var cleanDeps = new ConsumeJob {
                CmdBuffer = cmdBufferSystem.CreateCommandBuffer().ToConcurrent()
            }.ScheduleSingle(this, inputDeps);

            cmdBufferSystem.AddJobHandleForProducer(cleanDeps);
            return cleanDeps;
        }
    }

    /// <summary>
    /// Scales all the canvases if the resolution of the window changes.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(ConsumeChangeEvtSystem))]
    public class CanvasScalerSystem : JobComponentSystem {

        private struct ResizeCanvasJob : IJobForEach<ReferenceResolution, WidthHeightWeight, LocalToWorld> {

            public int2 Resolution;

            public void Execute([ReadOnly] ref ReferenceResolution c0, [ReadOnly] ref WidthHeightWeight c1, ref LocalToWorld c2) {
                var logWidth  = math.log2(Resolution.x / c0.Value.x);
                var logHeight = math.log2(Resolution.y / c0.Value.y);
                var avg       = math.lerp(logWidth, logHeight, c1.Value);
                var scale     = math.pow(2, avg);
                var center    = new float3(Resolution / 2, 0);
                c2            = new LocalToWorld { Value = float4x4.TRS(center, c2.Rotation, new float3(scale)) };
            }
        }

        private struct ProduceJob : IJob {

            public EntityCommandBuffer CmdBuffer;

            public void Execute() {
                var entity = CmdBuffer.CreateEntity();
                CmdBuffer.AddComponent<ResolutionChangeEvt>(entity);
            }
        }

        private EntityCommandBufferSystem cmdBufferSystem;
        private EntityQuery scaleQuery;
        private int2 res;

        protected override void OnCreate() {
            scaleQuery = GetEntityQuery(new EntityQueryDesc {
                All = new [] { 
                    ComponentType.ReadOnly<ReferenceResolution>(),
                    ComponentType.ReadOnly<WidthHeightWeight>(), 
                    ComponentType.ReadWrite<LocalToWorld>()
                }
            });

            cmdBufferSystem = World.GetOrCreateSystem<BeginPresentationEntityCommandBufferSystem>();
            res = new int2(Screen.width, Screen.height);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps) {
            var current = new int2(Screen.width, Screen.height);

            if (!res.Equals(current)) {
                res = current;
                var resizeDeps = new ResizeCanvasJob {
                    Resolution = current
                }.Schedule(this, inputDeps);

                var productionJob = new ProduceJob {
                    CmdBuffer = cmdBufferSystem.CreateCommandBuffer()
                }.Schedule(resizeDeps);

                cmdBufferSystem.AddJobHandleForProducer(productionJob);

                return productionJob;
            }

            return inputDeps;
        }
    }
}
