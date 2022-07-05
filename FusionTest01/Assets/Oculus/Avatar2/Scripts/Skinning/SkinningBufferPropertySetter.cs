using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Skinning
{
    // This class is required to workaround a bug/feature? when using Vulkan. If there are
    // ByteAddressBuffers or StructuredBuffers that exist in the shader (even if not used at runtime), they
    // must have their buffers set with something. This also causes issues with the Unity Editor that can't
    // be workaround here.

    // Has two "modes", one where dummy buffers are created and another where some are passed in
    internal class SkinningBufferPropertySetter : IDisposable
    {
        private static AttributePropertyIds _propertyIds = default;
        private IBufferHolder _bufferHolder;

        // Dummy buffers method
        public SkinningBufferPropertySetter()
        {
            CheckPropertyIdInit();
            _bufferHolder = new DummyBufferHolder();
        }

        public SkinningBufferPropertySetter(ComputeBuffer positionOutputBuffer, ComputeBuffer frenetOutputBuffer)
        {
            CheckPropertyIdInit();
            _bufferHolder = new BufferHolder(positionOutputBuffer, frenetOutputBuffer);
        }

        public void SetComputeSkinningBuffersInMatBlock(MaterialPropertyBlock matBlock)
        {
            matBlock.SetBuffer(_propertyIds.ComputeSkinnerPositionBuffer, _bufferHolder.PositionOutputBuffer);
            matBlock.SetBuffer(_propertyIds.ComputeSkinnerFrenetBuffer, _bufferHolder.FrenetOutputBuffer);
        }

        public void Dispose()
        {
            _bufferHolder.Dispose();
        }

        private static void CheckPropertyIdInit()
        {
            if (!_propertyIds.IsValid)
            {
                _propertyIds = new AttributePropertyIds(AttributePropertyIds.InitMethod.PropertyToId);
            }
        }

        //////////////////////////////////////////
        // Buffer Holder Interface and Classes //
        /////////////////////////////////////////
        private interface IBufferHolder : IDisposable
        {
            ComputeBuffer PositionOutputBuffer { get; }
            ComputeBuffer FrenetOutputBuffer { get; }
        }

        private class DummyBufferHolder : IBufferHolder
        {
            private ComputeBuffer _dummyBuffer;

            public DummyBufferHolder()
            {
                _dummyBuffer = new ComputeBuffer(1, sizeof(uint));
            }

            public void Dispose()
            {
                _dummyBuffer.Dispose();
            }

            public ComputeBuffer PositionOutputBuffer => _dummyBuffer;
            public ComputeBuffer FrenetOutputBuffer => _dummyBuffer;
        }

        private class BufferHolder : IBufferHolder
        {
            private ComputeBuffer _posBuffer;
            private ComputeBuffer _frenetBuffer;

            public BufferHolder(ComputeBuffer positionOutputBuffer, ComputeBuffer frenetOutputBuffer)
            {
                _posBuffer = positionOutputBuffer;
                _frenetBuffer = frenetOutputBuffer;
            }

            public void Dispose()
            {
                // Intentionally empty, does not own the buffers
            }

            public ComputeBuffer PositionOutputBuffer => _posBuffer;
            public ComputeBuffer FrenetOutputBuffer => _frenetBuffer;
        }

        //////////////////////////
        // AttributePropertyIds //
        //////////////////////////
        private struct AttributePropertyIds
        {
            public readonly int ComputeSkinnerPositionBuffer;
            public readonly int ComputeSkinnerFrenetBuffer;

            // These will both be 0 if default initialized, otherwise they are guaranteed unique
            public bool IsValid => ComputeSkinnerPositionBuffer != ComputeSkinnerFrenetBuffer;

            public enum InitMethod { PropertyToId }
            public AttributePropertyIds(InitMethod initMethod)
            {
                ComputeSkinnerPositionBuffer = Shader.PropertyToID("_OvrPositionBuffer");
                ComputeSkinnerFrenetBuffer = Shader.PropertyToID("_OvrFrenetBuffer");
            }
        }
    }
}
