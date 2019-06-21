﻿using Mafia2Tool;
using SharpDX.Direct3D11;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Utils.Settings;

namespace Rendering.Graphics
{
    public static class TextureLoader
    {
        private static string GetTextureFromPath(string fileName)
        {
            string path = "";
            if (!fileName.Contains(".ifl"))
            {
                path = Path.Combine(SceneData.ScenePath, fileName);
                if (File.Exists(path))
                {
                    string mip = Path.Combine(SceneData.ScenePath, "MIP_" + fileName);
                    if (File.Exists(mip) && ToolkitSettings.UseMIPS)
                        return mip;
                    else
                        return path;
                }

                path = Path.Combine(ToolkitSettings.TexturePath, fileName);
                if (File.Exists(path))
                {
                    string mip = Path.Combine(ToolkitSettings.TexturePath, "MIP_" + fileName);
                    if (File.Exists(mip) && ToolkitSettings.UseMIPS)
                        return mip;
                    else
                        return path;
                }
            }
            path = Path.Combine(ToolkitSettings.TexturePath, "texture.dds");
            if (File.Exists(path))
                return path;
            else
            {
                Debug.WriteLine("FAILED TO LOAD: {0}", fileName);
                throw new System.Exception("Unable to locate texture.dds! This should be located in the texture folder assigned. You can assign it in Options > Render > Texture Directory.");
            }
        }

        public static ShaderResourceView LoadTexture(Device d3d, DeviceContext d3dContext, string fileName)
        {
            Resource ddsResource;
            ShaderResourceView _temp;
            DDSTextureLoader.DDS_ALPHA_MODE mode;
            Debug.WriteLine(fileName);
            string texturePath = GetTextureFromPath(fileName);
            DDSTextureLoader.CreateDDSTextureFromFile(d3d, d3dContext, texturePath, out ddsResource, out _temp, 4096, out mode);
            return _temp;
        }
    }
}