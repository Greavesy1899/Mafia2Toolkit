﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Mafia2
{
    public class CustomEDM
    {
        string name;
        int partCount;
        Part[] parts;


        ulong bufferIndexHash;
        ulong bufferVertexHash;
        VertexFlags flags;
        float positionFactor;
        Vector3 positionOffset;
        IndexBuffer indexBuffer;
        VertexBuffer vertexBuffer;
        Bounds bounds;


        public string Name {
            get { return name; }
            set { name = value; }
        }
        public int PartCount {
            get { return partCount; }
            set { partCount = value; }
        }
        public Part[] Parts {
            get { return parts; }
            set { parts = value; }
        }

        //This part is todo with putting the data into buffers.
        public ulong BufferIndexHash {
            get { return bufferIndexHash; }
            set { bufferIndexHash = value; }
        }
        public ulong BufferVertexHash {
            get { return bufferVertexHash; }
            set { bufferVertexHash = value; }
        }
        public Bounds Bound {
            get { return bounds; }
            set { bounds = value; }
        }
        public VertexFlags BufferFlags {
            get { return flags; }
            set { flags = value; }
        }
        public float PositionFactor {
            get { return positionFactor; }
            set { positionFactor = value; }
        }
        public Vector3 PositionOffset {
            get { return positionOffset; }
            set { positionOffset = value; }
        }
        public IndexBuffer IndexBuffer {
            get { return indexBuffer; }
            set { indexBuffer = value; }
        }
        public VertexBuffer VertexBuffer {
            get { return vertexBuffer; }
            set { vertexBuffer = value; }
        }

        public CustomEDM(BinaryReader reader)
        {
            int size = reader.ReadByte();
            name = new string(reader.ReadChars(size));

            int partCount = reader.ReadInt32();
            parts = new Part[partCount];

            for(int i = 0; i != partCount; i++)
            {
                parts[i] = new Part(reader);
            }
        }

        public CustomEDM(string name, int numParts)
        {
            this.name = name;
            this.partCount = numParts;
            parts = new Part[numParts];
        }

        public void WriteToFile(BinaryWriter writer)
        {
            writer.Write((byte)name.Length);
            writer.Write(name.ToCharArray());
            writer.Write(partCount);
            for (int i = 0; i != partCount; i++)
                parts[i].WriteToFile(writer);
        }

        /// <summary>
        /// Overwrite a part of the EDM. This is a better way to create a new part.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="indices"></param>
        /// <param name="name"></param>
        /// <param name="slot"></param>
        public void AddPart(List<Vertex> vertex, List<Short3> indices, string name, int slot)
        {
            parts[slot] = new Part(vertex, indices, name);
        }

        /// <summary>
        /// Build new buffers for new mesh. These will replace existing buffers.
        /// </summary>
        public void BuildBuffers()
        {
            indexBuffer = new IndexBuffer(bufferIndexHash);
            vertexBuffer = new VertexBuffer(bufferVertexHash);

            List<ushort> idata = new List<ushort>();
            List<byte> vdata = new List<byte>();

            bool hasTangents = false;
            for (int p = 0; p != parts.Length; p++)
            {
                for (int i = 0; i != parts[p].Indices.Count; i++)
                {
                    idata.Add((ushort)parts[p].Indices[i].S1);
                    idata.Add((ushort)parts[p].Indices[i].S2);
                    idata.Add((ushort)parts[p].Indices[i].S3);
                }
                for (int i = 0; i != parts[p].Vertices.Length; i++)
                {
                    byte tz = 0;
                    if (flags.HasFlag(VertexFlags.Position))
                    {
                        float x = parts[p].Vertices[i].X - positionOffset.X;
                        x = x / positionFactor;
                        float y = parts[p].Vertices[i].Y - positionOffset.Y;
                        y = y / positionFactor;
                        float z = parts[p].Vertices[i].Z - positionOffset.Z;
                        z = z / positionFactor;

                        ushort v1 = Convert.ToUInt16(x);
                        ushort v2 = Convert.ToUInt16(y);
                        ushort v3 = Convert.ToUInt16(z);

                        byte[] bytesv1 = BitConverter.GetBytes(v1);
                        byte[] bytesv2 = BitConverter.GetBytes(v2);
                        byte[] bytesv3 = BitConverter.GetBytes(v3);

                        vdata.Add(bytesv1[0]);
                        vdata.Add(bytesv1[1]);
                        vdata.Add(bytesv2[0]);
                        vdata.Add(bytesv2[1]);
                        vdata.Add(bytesv3[0]);
                        vdata.Add(bytesv3[1]);
                    }
                    if (flags.HasFlag(VertexFlags.Tangent))
                    {
                        hasTangents = true;

                        float fx = parts[p].Tangents[i].X * 127.0f + 127.0f;
                        float fy = parts[p].Tangents[i].Y * 127.0f + 127.0f;
                        float fz = parts[p].Tangents[i].Z * 127.0f + 127.0f;

                        if (float.IsNaN(fx))
                            fx = 0.0f;

                        if (float.IsNaN(fy))
                            fy = 0.0f;

                        if (float.IsNaN(fz))
                            fz = 0.0f;

                        byte x = Convert.ToByte(fx);
                        byte y = Convert.ToByte(fy);
                        tz = Convert.ToByte(fz);

                        vdata.Add(x);
                        vdata.Add(y);

                    }
                    if (flags.HasFlag(VertexFlags.Normals))
                    {
                        byte x = Convert.ToByte(parts[p].Normals[i].X * 127.0f + 127.0f);
                        byte y = Convert.ToByte(parts[p].Normals[i].Y * 127.0f + 127.0f);
                        byte z = Convert.ToByte(parts[p].Normals[i].Z * 127.0f + 127.0f);

                        vdata.Add(x);
                        vdata.Add(y);
                        vdata.Add(z);
                    }
                    if (hasTangents)
                    {
                        vdata.Add(tz);
                    }
                    if (flags.HasFlag(VertexFlags.TexCoords0))
                    {
                        byte[] x = Half.GetBytes(parts[p].UVs[i].X);
                        byte[] y = Half.GetBytes(parts[p].UVs[i].Y);

                        vdata.Add(x[0]);
                        vdata.Add(x[1]);
                        vdata.Add(y[0]);
                        vdata.Add(y[1]);
                    }
                }
            }
            indexBuffer.Data = idata.ToArray();
            vertexBuffer.Data = vdata.ToArray();
        }

        /// <summary>
        /// Calculate new bounds on the model.
        /// </summary>
        /// <param name="updateGeom">If this is true, then the "Position Offset" and "Position Scale will be updated."</param>
        public void CalculateBounds(bool updateGeom)
        {
            Vector3 min = new Vector3(0);
            Vector3 max = new Vector3(0);

            for (int p = 0; p != parts.Length; p++)
            {
                for (int i = 0; i != parts[p].Vertices.Length; i++)
                {
                    if (parts[p].Vertices[i].X < min.X)
                        min.X = parts[p].Vertices[i].X;

                    if (parts[p].Vertices[i].X > max.X)
                        max.X = parts[p].Vertices[i].X;

                    if (parts[p].Vertices[i].Y < min.Y)
                        min.Y = parts[p].Vertices[i].Y;

                    if (parts[p].Vertices[i].Y > max.Y)
                        max.Y = parts[p].Vertices[i].Y;

                    if (parts[p].Vertices[i].Z < min.Z)
                        min.Z = parts[p].Vertices[i].Z;

                    if (parts[p].Vertices[i].Z > max.Z)
                        max.Z = parts[p].Vertices[i].Z;
                }
            }

            bounds = new Bounds(min, max);

            if (updateGeom == false)
                return;

            float minFloatf = 0.000016f;
            Vector3 minFloat = new Vector3(minFloatf);
            Debug.WriteLine("min is: " + min);
            Debug.WriteLine("max is: " + max);
            min -= minFloat;
            max += minFloat;

            positionOffset = min;
            float fMaxSize = Math.Max(max.X - min.X + minFloatf, Math.Max(max.Y - min.Y + minFloatf, (max.Z - min.Y + minFloatf) * 2.0f));

            Debug.WriteLine("maxSize is: " + fMaxSize);

            //positionFactor = fMaxSize / 0x10000;
            positionFactor = (float)256 / 0x10000;
        }

        public class Part
        {
            string name;
            Vector3[] vertices;
            Vector3[] normals;
            Vector3[] tangents;
            UVVector2[] uvs;
            List<Short3> indices;
            bool hasNormals = true;
            bool hasTangents = true;
            bool hasUVs = true;

            public string Name {
                get { return name; }
                set { name = value; }
            }
            public Vector3[] Vertices {
                get { return vertices; }
                set { vertices = value; }
            }
            public Vector3[] Normals {
                get { return normals; }
                set { normals = value; }
            }
            public Vector3[] Tangents {
                get { return tangents; }
                set { tangents = value; }
            }
            public UVVector2[] UVs {
                get { return uvs; }
                set { uvs = value; }
            }
            public List<Short3> Indices {
                get { return indices; }
                set { indices = value; }
            }
            public bool HasNormals {
                get { return hasNormals; }
                set { hasNormals = value; }
            }
            public bool HasTangents {
                get { return hasTangents; }
                set { hasTangents = value; }
            }
            public bool HasUVs {
                get { return hasUVs; }
                set { hasUVs = value; }
            }

            /// <summary>
            /// Create an empty part.
            /// </summary>
            public Part() { }

            /// <summary>
            /// Create a part with vertexes, indices and a name.
            /// </summary>
            /// <param name="vertex">Vertexes from the buffers</param>
            /// <param name="indices">Indices from the parts</param>
            /// <param name="name">Name of the part</param>
            public Part(List<Vertex> vertex, List<Short3> indices, string name)
            {
                this.vertices = new Vector3[vertex.Count];

                if (vertex[0].Normal == null)
                    hasNormals = false;
                else
                    this.normals = new Vector3[vertex.Count];

                if (vertex[0].Tangent == null)
                    hasTangents = false;
                else
                    this.tangents = new Vector3[vertex.Count];

                if (vertex[0].UVs.Length == 0)
                    hasUVs = false;
                else
                    this.uvs = new UVVector2[vertex.Count];

                for (int i = 0; i != vertex.Count; i++)
                {
                    vertices[i] = vertex[i].Position;

                    if(hasNormals)
                        normals[i] = vertex[i].Normal;

                    if(hasTangents)
                        tangents[i] = vertex[i].Tangent;

                    if(hasUVs)
                        uvs[i] = vertex[i].UVs[0];
                }

                this.indices = indices;
                this.name = name;
            }

            /// <summary>
            /// Usually used for Collisions.
            /// </summary>
            /// <param name="vertex">Vertexes from the buffers</param>
            /// <param name="indices">Indices from the parts</param>
            /// <param name="name">Name of the part</param>
            public Part(Vector3[] vertices, Int3[] triangles, string name)
            {
                this.vertices = vertices;
                hasTangents = false;
                hasNormals = false;
                hasUVs = false;
                indices = new List<Short3>();

                foreach (Int3 tri in triangles)
                {
                    indices.Add(new Short3(tri.I1, tri.I2, tri.I3));
                }

                this.name = name;
            }

            /// <summary>
            /// Unknown
            /// </summary>
            /// <param name="vertex">Vertexes from the buffers</param>
            /// <param name="indices">Indices from the parts</param>
            /// <param name="name">Name of the part</param>
            public Part(Vector3[] vertices, Short3[] triangles, string name)
            {
                this.vertices = vertices;

                indices = new List<Short3>();

                foreach (Short3 tri in triangles)
                {
                    indices.Add(tri);
                }

                this.name = name;

                uvs = new UVVector2[0];
            }

            /// <summary>
            /// Read the EDM part from a mesh file. 
            /// </summary>
            /// <param name="reader"></param>
            public Part(BinaryReader reader)
            {
                ReadFromFile(reader);
            }

            /// <summary>
            /// Write part to file.
            /// </summary>
            /// <param name="writer"></param>
            public void WriteToFile(BinaryWriter writer)
            {
                if (name == null)
                    return;

                writer.Write(name);
                writer.Write(hasNormals);
                writer.Write(hasTangents);
                writer.Write(hasUVs);
                writer.Write(vertices.Length);

                for (int c = 0; c != vertices.Length; c++)
                    vertices[c].WriteToFile(writer);

                if (hasNormals)
                {
                    for (int c = 0; c != normals.Length; c++)
                        normals[c].WriteToFile(writer);
                }

                if (hasTangents)
                {
                    for (int c = 0; c != tangents.Length; c++)
                        tangents[c].WriteToFile(writer);
                }

                if (hasUVs)
                {
                    writer.Write(uvs.Length);
                    for (int c = 0; c != uvs.Length; c++)
                    {
                        writer.Write(uvs[c].X);
                        writer.Write(1f - uvs[c].Y);
                    }
                }

                writer.Write(indices.Count);
                for (int c = 0; c != indices.Count; c++)
                    indices[c].WriteToFile(writer);
            }

            /// <summary>
            /// Read part from file.
            /// </summary>
            /// <param name="reader"></param>
            public void ReadFromFile(BinaryReader reader)
            {
                byte nameSize = reader.ReadByte();
                name = new string(reader.ReadChars(nameSize));
                hasNormals = reader.ReadBoolean();
                hasTangents = reader.ReadBoolean();
                hasUVs = reader.ReadBoolean();
                int size = reader.ReadInt32();
                vertices = new Vector3[size];

                if(hasNormals)
                    normals = new Vector3[size];

                if(hasTangents)
                    tangents = new Vector3[size];

                for (int c = 0; c != vertices.Length; c++)
                    vertices[c] = new Vector3(reader);

                if (hasNormals)
                {
                    for (int c = 0; c != normals.Length; c++)
                        normals[c] = new Vector3(reader);
                }

                if (hasTangents)
                {
                    for (int c = 0; c != tangents.Length; c++)
                        tangents[c] = new Vector3(reader);
                }

                if (hasUVs)
                {
                    size = reader.ReadInt32();
                    uvs = new UVVector2[size];
                    for (int c = 0; c != uvs.Length; c++)
                    {
                        uvs[c] = new UVVector2(HalfHelper.SingleToHalf(reader.ReadSingle()), HalfHelper.SingleToHalf(reader.ReadSingle()));
                        uvs[c].Y = (Half)1f - uvs[c].Y;
                    }
                }
                size = reader.ReadInt32();
                indices = new List<Short3>();
                for (int c = 0; c != size; c++)
                    indices.Add(new Short3(reader));
            }
        }
    }
}
