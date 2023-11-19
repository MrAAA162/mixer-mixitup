﻿using CacheManager.Core;
using MixItUp.Base.Services;
using StreamingClient.Base.Util;
using StreamingClient.Base.Web;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using XamlAnimatedGif;

namespace MixItUp.WPF.Util
{
    public static class ImageHelper
    {
        private static ICacheManager<WriteableBitmap> bitmapCache = CacheFactory.Build<WriteableBitmap>(settings => settings
                                                                                                        .WithSystemRuntimeCacheHandle()
                                                                                                        .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(30)));

        private static ICacheManager<byte[]> gifCache = CacheFactory.Build<byte[]>(settings => settings
                                                                                   .WithSystemRuntimeCacheHandle()
                                                                                   .WithExpiration(ExpirationMode.Sliding, TimeSpan.FromMinutes(30)));

        public static void SetImageSource(Image image, string path, double width, double height, string tooltip = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && path.Length > 0)
                {
                    WriteableBitmap writeableBitmap = ImageHelper.bitmapCache.Get(path);
                    if (writeableBitmap != null)
                    {
                        ImageHelper.SetImageSource(image, width, height, tooltip, writeableBitmap);
                        return;
                    }

                    if (path.StartsWith("http"))
                    {
                        Task.Run(async () =>
                        {
                            byte[] bytes = null;
                            using (AdvancedHttpClient client = new AdvancedHttpClient())
                            {
                                bytes = await client.GetByteArrayAsync(path);
                            }
                            await Application.Current.Dispatcher.InvokeAsync(() => ImageHelper.AddImageToCacheAndSetImageSourceFromBytes(image, path, width, height, tooltip, bytes));
                        });
                    }
                    else if (ServiceManager.Get<IFileService>().FileExists(path))
                    {
                        Task.Run(async () =>
                        {
                            byte[] bytes = await ServiceManager.Get<IFileService>().ReadFileAsBytes(path);
                            await Application.Current.Dispatcher.InvokeAsync(() => ImageHelper.AddImageToCacheAndSetImageSourceFromBytes(image, path, width, height, tooltip, bytes));
                        });
                    }
                    else
                    {
                        ImageHelper.AddImageToCacheAndSetImageSource(image, path, width, height, tooltip, BitmapFactory.FromResource(path));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(path + " - " + ex);
            }
        }

        public static void SetGifSource(Image image, string path, double width, double height, string tooltip = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && path.Length > 0)
                {
                    byte[] bytes = ImageHelper.gifCache.Get(path);
                    if (bytes != null)
                    {
                        ImageHelper.SetGifSource(image, width, height, tooltip, bytes);
                        return;
                    }

                    if (path.StartsWith("http"))
                    {
                        Task.Run(async () =>
                        {
                            bytes = null;
                            using (AdvancedHttpClient client = new AdvancedHttpClient())
                            {
                                bytes = await client.GetByteArrayAsync(path);
                            }
                            await Application.Current.Dispatcher.InvokeAsync(() => ImageHelper.AddGifToCacheAndSetImageBytes(image, path, width, height, tooltip, bytes));
                        });
                    }
                    else if (ServiceManager.Get<IFileService>().FileExists(path))
                    {
                        Task.Run(async () =>
                        {
                            bytes = await ServiceManager.Get<IFileService>().ReadFileAsBytes(path);
                            await Application.Current.Dispatcher.InvokeAsync(() => ImageHelper.AddGifToCacheAndSetImageBytes(image, path, width, height, tooltip, bytes));
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(path + " - " + ex);
            }
        }

        private static void AddImageToCacheAndSetImageSourceFromBytes(Image image, string id, double width, double height, string tooltip, byte[] bytes)
        {
            try
            {
                if (bytes != null && bytes.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(bytes))
                    {
                        BitmapImage bitmapImage = new BitmapImage();
                        bitmapImage.BeginInit();
                        bitmapImage.DecodePixelWidth = (int)width;
                        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                        bitmapImage.CreateOptions = BitmapCreateOptions.None;
                        bitmapImage.StreamSource = stream;
                        bitmapImage.EndInit();

                        if (bitmapImage.CanFreeze)
                        {
                            bitmapImage.Freeze();
                        }

                        ImageHelper.AddImageToCacheAndSetImageSource(image, id, width, height, tooltip, new WriteableBitmap(bitmapImage));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private static void AddImageToCacheAndSetImageSource(Image image, string id, double width, double height, string tooltip, WriteableBitmap writeableBitmap)
        {
            ImageHelper.bitmapCache.Put(id, writeableBitmap);
            ImageHelper.SetImageSource(image, width, height, tooltip, writeableBitmap);
        }

        private static void SetImageSource(Image image, double width, double height, string tooltip, WriteableBitmap writeableBitmap)
        {
            if (writeableBitmap != null)
            {
                image.Width = width;
                image.Height = height;
                image.Source = writeableBitmap;
                if (!string.IsNullOrEmpty(tooltip))
                {
                    image.ToolTip = tooltip;
                }
            }
        }

        private static void AddGifToCacheAndSetImageBytes(Image image, string id, double width, double height, string tooltip, byte[] bytes)
        {
            ImageHelper.gifCache.Put(id, bytes);
            ImageHelper.SetGifSource(image, width, height, tooltip, bytes);
        }

        private static void SetGifSource(Image image, double width, double height, string tooltip, byte[] bytes)
        {
            if (bytes != null)
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    image.Width = width;
                    image.Height = height;
                    AnimationBehavior.SetSourceStream(image, stream);
                    AnimationBehavior.SetRepeatBehavior(image, new RepeatBehavior(20));
                    if (!string.IsNullOrEmpty(tooltip))
                    {
                        image.ToolTip = tooltip;
                    }
                }
            }
        }
    }
}