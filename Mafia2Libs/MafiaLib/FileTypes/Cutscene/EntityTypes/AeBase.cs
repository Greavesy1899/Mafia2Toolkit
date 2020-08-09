﻿using ResourceTypes.Cutscene.KeyParams;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Utils.Extensions;

namespace ResourceTypes.Cutscene.AnimEntities
{
    public abstract class AeBase
    {
        [TypeConverter(typeof(ExpandableObjectConverter))]
        public AeBaseData EntityData { get; protected set; }
        public abstract int GetEntityDefinitionType();
        public abstract int GetEntityDataType();
        public abstract bool ReadDefinitionFromFile(MemoryStream stream, bool isBigEndian);
        public abstract bool WriteDefinitionToFile(MemoryStream writer, bool isBigEndian);
        public abstract bool ReadDataFromFile(MemoryStream stream, bool isBigEndian);
        public abstract bool WriteDataFromFile(MemoryStream stream, bool isBigEndian);

    }

    public class AeBaseData
    {
        [Browsable(false)]
        public int DataType { get; set; } // We might need an enumator for this
        [Browsable(false)]
        public int Size { get; set; } // Total Size of the data. includes Size and DataType.
        [Browsable(false)]
        public int KeyDataSize { get; set; } // Size of all the keyframes? Also count and the Unk01?
        public int Unk00 { get; set; }
        public int Unk01 { get; set; }
        public int NumKeyFrames { get; set; } // Number of keyframes. Start with 0xE803 or 1000
        public IKeyType[] KeyFrames { get; set; }
        //public int Unk02 { get; set; } // 0x70 - 112
        //public int Unk03 { get; set; } // 0x15 - 21
        //public float Unk04 { get; set; } // 0x803F - 1.0f;
        //public int Unk05 { get; set; } // 0x0
        //public int Unk06 { get; set; } // 0xFFFF
        //public byte Unk07 { get; set; } // 0x0

        public virtual void ReadFromFile(MemoryStream stream, bool isBigEndian)
        {
            DataType = stream.ReadInt32(isBigEndian);
            Size = stream.ReadInt32(isBigEndian);
            Unk00 = stream.ReadInt32(isBigEndian);
            KeyDataSize = stream.ReadInt32(isBigEndian);
            Unk01 = stream.ReadInt32(isBigEndian);
            NumKeyFrames = stream.ReadInt32(isBigEndian);

            KeyFrames = new IKeyType[NumKeyFrames];

            for (int i = 0; i < NumKeyFrames; i++)
            {
                Debug.Assert(stream.Position != stream.Length, "Reached the end to early?");

                int Header = stream.ReadInt32(isBigEndian);
                Debug.Assert(Header == 1000, "Keyframe magic did not equal 1000");
                int Size = stream.ReadInt32(isBigEndian);
                int KeyType = stream.ReadInt32(isBigEndian);
                AnimKeyParamTypes KeyParamType = (AnimKeyParamTypes)KeyType;

                IKeyType KeyParam = CutsceneKeyParamFactory.ReadAnimEntityFromFile(KeyParamType, Size, stream);
                KeyFrames[i] = KeyParam;
            }
        }

        public virtual void WriteToFile(MemoryStream stream, bool isBigEndian)
        {
        }

        public override string ToString()
        {
            return string.Format("{0}", DataType);
        }
    }
}
