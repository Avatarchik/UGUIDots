using System;
using Unity.Entities;
using UnityEngine;

namespace UGUIDots.Render {

    // TODO: Convert to using a pointer?
    public struct RenderCommand : ISharedComponentData, IEquatable<RenderCommand> {
        public OrthographicRenderFeature RenderFeature;

        public bool Equals(RenderCommand other) {
            return other.RenderFeature == RenderFeature;
        }

        public override int GetHashCode() {
            var hash = 0;
            if (RenderFeature != null) hash ^= RenderFeature.GetHashCode();
            return hash;
        }
    }

    public class RenderCommandProxy : MonoBehaviour, IConvertGameObjectToEntity {

        public OrthographicRenderFeature RenderFeature;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem) {
            dstManager.AddSharedComponentData(entity, new RenderCommand {
                RenderFeature = RenderFeature
            });
        }
    }
}
