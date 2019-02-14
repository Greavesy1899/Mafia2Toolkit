﻿using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System.Windows.Forms;

namespace Rendering.Graphics
{
    public class RenderBoundingBox
    {
        public BoundingBox Boundings { get; private set; }
        public ShaderResourceView Texture { get; private set; }

        public Buffer VertexBuffer { get; private set; }
        public Buffer IndexBuffer { get; private set; }
        private VertexLayouts.NormalLayout.Vertex[] Vertices { get; set; }
        public  ushort[] Indices { get; private set; }

        public RenderBoundingBox()
        {

        }

        public bool Init(BoundingBox bbox)
        {
            Boundings = bbox;

            Vertices = new VertexLayouts.NormalLayout.Vertex[8];
            //1
            Vertices[0].Position = new Vector3(Boundings.Minimum.X, Boundings.Minimum.X, Boundings.Minimum.Z);
            Vertices[0].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[0].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[0].TexCoord7 = new Vector2(0.0f, 1.0f);

            //2
            Vertices[1].Position = new Vector3(Boundings.Maximum.X, Boundings.Minimum.X, Boundings.Minimum.Z);
            Vertices[1].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[1].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[1].TexCoord7 = new Vector2(0.0f, 1.0f);

            //3
            Vertices[2].Position = new Vector3(Boundings.Minimum.X, Boundings.Minimum.X, Boundings.Maximum.Z);
            Vertices[2].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[2].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[2].TexCoord7 = new Vector2(0.0f, 1.0f);

            //4
            Vertices[3].Position = new Vector3(Boundings.Maximum.X, Boundings.Minimum.X, Boundings.Maximum.Z);
            Vertices[3].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[3].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[3].TexCoord7 = new Vector2(0.0f, 1.0f);

            //5
            Vertices[4].Position = new Vector3(Boundings.Minimum.X, Boundings.Maximum.Y, Boundings.Minimum.Z);
            Vertices[4].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[4].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[4].TexCoord7 = new Vector2(0.0f, 1.0f);

            //6
            Vertices[5].Position = new Vector3(Boundings.Maximum.X, Boundings.Maximum.Y, Boundings.Minimum.Z);
            Vertices[5].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[5].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[5].TexCoord7 = new Vector2(0.0f, 1.0f);

            //7
            Vertices[6].Position = new Vector3(Boundings.Minimum.X, Boundings.Maximum.Y, Boundings.Maximum.Z);
            Vertices[6].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[6].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[6].TexCoord7 = new Vector2(0.0f, 1.0f);

            //8
            Vertices[7].Position = new Vector3(Boundings.Maximum.X, Boundings.Maximum.Y, Boundings.Maximum.Z);
            Vertices[7].Normal = new Vector3(0.0f, 0.0f, 0.0f);
            Vertices[7].TexCoord0 = new Vector2(0.0f, 1.0f);
            Vertices[7].TexCoord7 = new Vector2(0.0f, 1.0f);

            Indices = new ushort[] {
                // front
                0,2,3,
                3,1,0,
                // back
                4,5,7,
                7,6,4,
                // left
                0,1,4,
                5,4,0,
                // right
                1,3,7,
                7,5,1,
                //top
                3,2,6,
                6,7,3,
                // bottom
                2,0,4,
                4,6,2,
            };

            return true;
        }

        public bool InitBuffer(Device device)
        {
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, Vertices);
            IndexBuffer = Buffer.Create(device, BindFlags.IndexBuffer, Indices);
            return true;
        }

        public bool Render(DeviceContext deviceContext)
        {
            RenderBuffers(deviceContext);
            return true;
        }

        private bool LoadTexture(Device device)
        {
            TextureClass Texture = new TextureClass();
            bool result = Texture.Init(device, "texture.dds");
            this.Texture = Texture.TextureResource;
            return true;
        }

        public void ReleaseModel()
        {
            Vertices = null;
            Indices = null;
        }

        public void ReleaseTextures()
        {
            Texture?.Dispose();
            Texture = null;
        }

        public void ShutdownBuffers()
        { 
            IndexBuffer?.Dispose();
            IndexBuffer = null;
            VertexBuffer?.Dispose();
            VertexBuffer = null;
        }
        private void RenderBuffers(DeviceContext deviceContext)
        {
            deviceContext.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(VertexBuffer, Utilities.SizeOf<VertexLayouts.NormalLayout.Vertex>(), 0));
            deviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
        }


    }
}
