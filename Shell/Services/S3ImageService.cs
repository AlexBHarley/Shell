﻿using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace Shell.Services
{
    public class S3ImageService : IImageService
    {
        string imagesBucket = "shell-product-images";

        IAmazonS3 _client;

        public string UploadImages(List<Tuple<HttpPostedFileBase, bool>> files, int orgId, int productId)
        {
            string displayImageName = "";

            using (_client = new AmazonS3Client())
            {
                try
                {
                    foreach(var file in files)
                    {
                        var key = string.Format("{0}/{1}/{2}", orgId, productId, Guid.NewGuid());

                        if (file.Item2)
                        {
                            displayImageName = key;
                        }
                        var request = new PutObjectRequest
                        {
                            Key = key,
                            BucketName = imagesBucket,
                            InputStream = file.Item1.InputStream
                        };
                        _client.PutObject(request);
                    }
                    return displayImageName;
                }
                catch
                {
                    //Investigate how to properly propogate exceptions
                    throw;
                }
            }
        }

        public List<string> GetImageURLs(int orgId, int productId)
        {
            using (_client = new AmazonS3Client())
            {
                ListObjectsRequest objectRequest = new ListObjectsRequest
                {
                    BucketName = imagesBucket,
                    Prefix = string.Format("{0}/{1}/", orgId, productId)
                };
                var images = _client.ListObjects(objectRequest);
                var urls = new List<string>();

                foreach (var image in images.S3Objects)
                {
                    var request = new GetPreSignedUrlRequest
                    {
                        BucketName = imagesBucket,
                        Key = image.Key,
                        Expires = DateTime.Now.AddMinutes(5)
                    };
                    string url = _client.GetPreSignedURL(request);
                    urls.Add(url);
                }
                return urls;
            }
        }

        public void DeleteImages(int orgId, int productId)
        {
            var listRequest = new ListObjectsRequest
            {
                BucketName = imagesBucket,
                Prefix = string.Format("{0}/{1}", orgId, productId)
            };

            using (_client = new AmazonS3Client())
            {
                var keys = _client.ListObjects(listRequest);
                var keyVersions = new List<KeyVersion>();

                keys.S3Objects.ForEach(i => keyVersions.Add(
                    new KeyVersion
                    {
                        Key = i.Key,
                        VersionId = null
                    }
                ));

                var request = new DeleteObjectsRequest
                {
                    BucketName = imagesBucket,
                    Objects = keyVersions
                };
                _client.DeleteObjects(request);
            }
        }
    }
}