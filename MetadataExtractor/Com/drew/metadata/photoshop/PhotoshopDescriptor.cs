/*
 * Copyright 2002-2015 Drew Noakes
 *
 *    Modified by Yakov Danilov <yakodani@gmail.com> for Imazen LLC (Ported from Java to C#)
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * More information about this project is available at:
 *
 *    https://drewnoakes.com/code/exif/
 *    https://github.com/drewnoakes/metadata-extractor
 */

using System;
using System.IO;
using System.Text;
using Com.Drew.Lang;
using JetBrains.Annotations;

namespace Com.Drew.Metadata.Photoshop
{
    /// <author>Yuri Binev</author>
    /// <author>Drew Noakes https://drewnoakes.com</author>
    public sealed class PhotoshopDescriptor : TagDescriptor<PhotoshopDirectory>
    {
        public PhotoshopDescriptor([NotNull] PhotoshopDirectory directory)
            : base(directory)
        {
        }

        public override string GetDescription(int tagType)
        {
            switch (tagType)
            {
                case PhotoshopDirectory.TagThumbnail:
                case PhotoshopDirectory.TagThumbnailOld:
                {
                    return GetThumbnailDescription(tagType);
                }

                case PhotoshopDirectory.TagUrl:
                case PhotoshopDirectory.TagXml:
                {
                    return GetSimpleString(tagType);
                }

                case PhotoshopDirectory.TagIptc:
                {
                    return GetBinaryDataString(tagType);
                }

                case PhotoshopDirectory.TagSlices:
                {
                    return GetSlicesDescription();
                }

                case PhotoshopDirectory.TagVersion:
                {
                    return GetVersionDescription();
                }

                case PhotoshopDirectory.TagCopyright:
                {
                    return GetBooleanString(tagType);
                }

                case PhotoshopDirectory.TagResolutionInfo:
                {
                    return GetResolutionInfoDescription();
                }

                case PhotoshopDirectory.TagGlobalAngle:
                case PhotoshopDirectory.TagGlobalAltitude:
                case PhotoshopDirectory.TagUrlList:
                case PhotoshopDirectory.TagSeedNumber:
                {
                    return Get32BitNumberString(tagType);
                }

                case PhotoshopDirectory.TagJpegQuality:
                {
                    return GetJpegQualityString();
                }

                case PhotoshopDirectory.TagPrintScale:
                {
                    return GetPrintScaleDescription();
                }

                case PhotoshopDirectory.TagPixelAspectRatio:
                {
                    return GetPixelAspectRatioString();
                }

                default:
                {
                    return base.GetDescription(tagType);
                }
            }
        }

        [CanBeNull]
        public string GetJpegQualityString()
        {
            try
            {
                var b = Directory.GetByteArray(PhotoshopDirectory.TagJpegQuality);
                if (b == null)
                {
                    return Directory.GetString(PhotoshopDirectory.TagJpegQuality);
                }
                IndexedReader reader = new ByteArrayReader(b);
                int q = reader.GetUInt16(0);
                // & 0xFFFF;
                int f = reader.GetUInt16(2);
                // & 0xFFFF;
                int s = reader.GetUInt16(4);
                int q1;
                if (q <= unchecked(0xFFFF) && q >= unchecked(0xFFFD))
                {
                    q1 = q - unchecked(0xFFFC);
                }
                else
                {
                    if (q <= 8)
                    {
                        q1 = q + 4;
                    }
                    else
                    {
                        q1 = q;
                    }
                }
                string quality;
                switch (q)
                {
                    case unchecked(0xFFFD):
                    case unchecked(0xFFFE):
                    case unchecked(0xFFFF):
                    case 0:
                    {
                        quality = "Low";
                        break;
                    }

                    case 1:
                    case 2:
                    case 3:
                    {
                        quality = "Medium";
                        break;
                    }

                    case 4:
                    case 5:
                    {
                        quality = "High";
                        break;
                    }

                    case 6:
                    case 7:
                    case 8:
                    {
                        quality = "Maximum";
                        break;
                    }

                    default:
                    {
                        quality = "Unknown";
                        break;
                    }
                }
                string format;
                switch (f)
                {
                    case unchecked(0x0000):
                    {
                        format = "Standard";
                        break;
                    }

                    case unchecked(0x0001):
                    {
                        format = "Optimised";
                        break;
                    }

                    case unchecked(0x0101):
                    {
                        format = "Progressive ";
                        break;
                    }

                    default:
                    {
                        format = string.Format("Unknown (0x{0:X4})", f);
                        break;
                    }
                }
                var scans = s >= 1 && s <= 3 ? (s + 2).ToString() : string.Format("Unknown (0x{0:X4})", s);
                return string.Format("{0} ({1}), {2} format, {3} scans", q1, quality, format, scans);
            }
            catch (IOException)
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetPixelAspectRatioString()
        {
            try
            {
                var bytes = Directory.GetByteArray(PhotoshopDirectory.TagPixelAspectRatio);
                if (bytes == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(bytes);
                var d = reader.GetDouble64(4);
                return ((object)d).ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetPrintScaleDescription()
        {
            try
            {
                var bytes = Directory.GetByteArray(PhotoshopDirectory.TagPrintScale);
                if (bytes == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(bytes);
                var style = reader.GetInt32(0);
                var locX = reader.GetFloat32(2);
                var locY = reader.GetFloat32(6);
                var scale = reader.GetFloat32(10);
                switch (style)
                {
                    case 0:
                    {
                        return "Centered, Scale " + scale;
                    }

                    case 1:
                    {
                        return "Size to fit";
                    }

                    case 2:
                    {
                        return string.Format("User defined, X:{0} Y:{1}, Scale:{2}", locX, locY, scale);
                    }

                    default:
                    {
                        return string.Format("Unknown {0:X4}, X:{1} Y:{2}, Scale:{3}", style, locX, locY, scale);
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetResolutionInfoDescription()
        {
            try
            {
                var bytes = Directory.GetByteArray(PhotoshopDirectory.TagResolutionInfo);
                if (bytes == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(bytes);
                var resX = reader.GetS15Fixed16(0);
                var resY = reader.GetS15Fixed16(8);
                // is this the correct offset? it's only reading 4 bytes each time
                return resX + "x" + resY + " DPI";
            }
            catch (Exception)
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetVersionDescription()
        {
            try
            {
                var bytes = Directory.GetByteArray(PhotoshopDirectory.TagVersion);
                if (bytes == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(bytes);
                var pos = 0;
                var ver = reader.GetInt32(0);
                pos += 4;
                pos++;
                var readerLength = reader.GetInt32(5);
                pos += 4;
                var readerStr = reader.GetString(9, readerLength * 2, Encoding.Unicode);
                pos += readerLength * 2;
                var writerLength = reader.GetInt32(pos);
                pos += 4;
                var writerStr = reader.GetString(pos, writerLength * 2, Encoding.Unicode);
                pos += writerLength * 2;
                var fileVersion = reader.GetInt32(pos);
                return string.Format("{0} ({1}, {2}) {3}", ver, readerStr, writerStr, fileVersion);
            }
            catch (IOException)
            {
                return null;
            }
        }

        [CanBeNull]
        public string GetSlicesDescription()
        {
            try
            {
                var bytes = Directory.GetByteArray(PhotoshopDirectory.TagSlices);
                if (bytes == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(bytes);
                var nameLength = reader.GetInt32(20);
                var name = reader.GetString(24, nameLength * 2, Encoding.Unicode);
                var pos = 24 + nameLength * 2;
                var sliceCount = reader.GetInt32(pos);
                //pos += 4;
                return string.Format("{0} ({1},{2},{3},{4}) {5} Slices", name, reader.GetInt32(4), reader.GetInt32(8), reader.GetInt32(12), reader.GetInt32(16), sliceCount);
            }
            catch (IOException)
            {
            /*for (int i=0;i<sliceCount;i++){
                pos+=16;
                int slNameLen=getInt32(b,pos);
                pos+=4;
                String slName=new String(b, pos, slNameLen*2,"UTF-16");
                res+=slName;
            }*/
                return null;
            }
        }

        [CanBeNull]
        public string GetThumbnailDescription(int tagType)
        {
            try
            {
                var v = Directory.GetByteArray(tagType);
                if (v == null)
                {
                    return null;
                }
                IndexedReader reader = new ByteArrayReader(v);
                //int pos = 0;
                var format = reader.GetInt32(0);
                //pos += 4;
                var width = reader.GetInt32(4);
                //pos += 4;
                var height = reader.GetInt32(8);
                //pos += 4;
                //pos += 4; //skip WidthBytes
                var totalSize = reader.GetInt32(16);
                //pos += 4;
                var compSize = reader.GetInt32(20);
                //pos += 4;
                var bpp = reader.GetInt32(24);
                //pos+=2;
                //pos+=2; //skip Number of planes
                //int thumbSize=v.length-pos;
                return string.Format("{0}, {1}x{2}, Decomp {3} bytes, {4} bpp, {5} bytes", format == 1 ? "JpegRGB" : "RawRGB", width, height, totalSize, bpp, compSize);
            }
            catch (IOException)
            {
                return null;
            }
        }

        [CanBeNull]
        private string GetBooleanString(int tag)
        {
            var bytes = Directory.GetByteArray(tag);
            if (bytes == null)
            {
                return null;
            }
            return bytes[0] == 0 ? "No" : "Yes";
        }

        [CanBeNull]
        private string Get32BitNumberString(int tag)
        {
            var bytes = Directory.GetByteArray(tag);
            if (bytes == null)
            {
                return null;
            }
            IndexedReader reader = new ByteArrayReader(bytes);
            try
            {
                return string.Format("{0}", reader.GetInt32(0));
            }
            catch (IOException)
            {
                return null;
            }
        }

        [CanBeNull]
        private string GetSimpleString(int tagType)
        {
            var bytes = Directory.GetByteArray(tagType);
            if (bytes == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(bytes);
        }

        [CanBeNull]
        private string GetBinaryDataString(int tagType)
        {
            var bytes = Directory.GetByteArray(tagType);
            if (bytes == null)
            {
                return null;
            }
            return string.Format("{0} bytes binary data", bytes.Length);
        }
    }
}