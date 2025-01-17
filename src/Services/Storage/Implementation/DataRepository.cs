using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

using Altinn.Platform.Storage.Interface.Enums;
using Altinn.Platform.Storage.Interface.Models;
using Altinn.Platform.Storage.Repository;

using LocalTest.Configuration;

using Microsoft.Extensions.Options;

using Newtonsoft.Json;

namespace LocalTest.Services.Storage.Implementation
{
    public class DataRepository : IDataRepository
    {
        private readonly LocalPlatformSettings _localPlatformSettings;

        public DataRepository(IOptions<LocalPlatformSettings> localPlatformSettings)
        {
            _localPlatformSettings = localPlatformSettings.Value;
        }

        public async Task<DataElement> Create(DataElement dataElement)
        {
            string path = GetDataPath(dataElement.InstanceGuid, dataElement.Id);

            Directory.CreateDirectory(GetDataCollectionFolder());
            Directory.CreateDirectory(GetDataForInstanceFolder(dataElement.InstanceGuid));

            await WriteToFile(path, dataElement.ToString());

            return dataElement;
        }

        public Task<bool> Delete(DataElement dataElement)
        {
            string path = GetDataPath(dataElement.InstanceGuid, dataElement.Id);
            File.Delete(path);
            return Task.FromResult(true);
        }

        public Task<bool> DeleteDataInStorage(string org, string blobStoragePath)
        {
            string path = GetFilePath(blobStoragePath);
            File.Delete(path);
            return Task.FromResult(true);
        }

        public async Task<DataElement> Read(Guid instanceGuid, Guid dataElementId)
        {
            string dataPath = GetDataPath(instanceGuid.ToString(), dataElementId.ToString());
            string content = await ReadFileAsString(dataPath);
            DataElement dataElement = (DataElement)JsonConvert.DeserializeObject(content, typeof(DataElement));
            return dataElement;
        }

        public async Task<List<DataElement>> ReadAll(Guid instanceGuid)
        {
            List<DataElement> dataElements = new List<DataElement>();
            string path = GetDataForInstanceFolder(instanceGuid.ToString());
            if (Directory.Exists(path))
            {
                string[] files = Directory.GetFiles(path);
                foreach (string filePath in files)
                {
                    string content = await ReadFileAsString(filePath);
                    DataElement instance = (DataElement)JsonConvert.DeserializeObject(content, typeof(DataElement));
                    dataElements.Add(instance);
                }
            }
            return dataElements;
        }

        public async Task<Stream> ReadDataFromStorage(string org, string blobStoragePath)
        {
            string filePath = GetFilePath(blobStoragePath);

            return await ReadFileAsStream(filePath);
        }

        public async Task<DataElement> Update(DataElement dataElement)
        {
            string path = GetDataPath(dataElement.InstanceGuid, dataElement.Id);

            Directory.CreateDirectory(GetDataCollectionFolder());
            Directory.CreateDirectory(GetDataForInstanceFolder(dataElement.InstanceGuid));

            await WriteToFile(path, dataElement.ToString());

            return dataElement;
        }

        public async Task<DataElement> Update(Guid instanceGuid, Guid dataElementId, Dictionary<string, object> propertylist)
        {
            string path = GetDataPath($"{instanceGuid}", $"{dataElementId}");

            if (File.Exists(path))
            {
                string content = await ReadFileAsString(path);
                DataElement dataElement = JsonConvert.DeserializeObject<DataElement>(content);

                foreach (KeyValuePair<string, object> property in propertylist)
                {
                    string propName = property.Key.Trim('/');
                    switch (propName)
                    {
                        case "contentType":
                            {
                                dataElement.ContentType = (string)property.Value;
                                break;
                            }
                        case "deleteStatus":
                            {
                                dataElement.DeleteStatus = (DeleteStatus)property.Value;
                                break;
                            }
                        case "filename":
                            {
                                dataElement.Filename = (string)property.Value;
                                break;
                            }
                        case "fileScanResult":
                            {
                                dataElement.FileScanResult = (FileScanResult)property.Value;
                                break;
                            }
                        case "isRead":
                            {
                                dataElement.IsRead = (bool)property.Value;
                                break;
                            }
                        case "lastChangedBy":
                            {
                                dataElement.LastChangedBy = (string)property.Value;
                                break;
                            }
                        case "lastChanged":
                            {
                                dataElement.LastChanged = (DateTime)property.Value;
                                break;
                            }
                        case "locked":
                            {
                                dataElement.Locked = (bool)property.Value;
                                break;
                            }
                        case "refs":
                            {
                                dataElement.Refs = (List<Guid>)property.Value;
                                break;
                            }
                        case "references":
                            {
                                dataElement.References = (List<Reference>)property.Value;
                                break;
                            }
                        case "size":
                            {
                                dataElement.Size = (long)property.Value;
                                break;
                            }
                        case "tags":
                            {
                                dataElement.Tags = (List<string>)property.Value;
                                break;
                            }
                        default:
                            break;
                    }
                }
                await Update(dataElement);

                return dataElement;
            }

            throw new RepositoryException("Error occured");
        }

        public async Task<(long ContentLength, DateTimeOffset LastModified)> WriteDataToStorage(string org, Stream stream, string blobStoragePath)
        {
            string filePath = GetFilePath(blobStoragePath);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }

            return await WriteToFile(filePath, stream);
        }

        private string GetFilePath(string fileName)
        {
            return _localPlatformSettings.LocalTestingStorageBasePath + _localPlatformSettings.BlobStorageFolder + fileName;
        }

        private string GetDataPath(string instanceId, string dataId)
        {
            return Path.Combine(GetDataForInstanceFolder(instanceId) + dataId.Replace("/", "_") + ".json");
        }

        private string GetDataForInstanceFolder(string instanceId)
        {
            return Path.Combine(GetDataCollectionFolder() + instanceId.Replace("/", "_") + "/");
        }

        private string GetDataCollectionFolder()
        {
            return this._localPlatformSettings.LocalTestingStorageBasePath + this._localPlatformSettings.DocumentDbFolder + this._localPlatformSettings.DataCollectionFolder;
        }

        private static async Task<string> ReadFileAsString(string path)
        {
            Stream stream = await ReadFileAsStream(path);
            using StreamReader reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }

        private static async Task<Stream> ReadFileAsStream(string path)
        {
            try
            {
                return ReadFileAsStreamInternal(path);
            }
            catch (IOException ioException)
            {
                if (ioException.Message.Contains("used by another process"))
                {
                    await Task.Delay(400);
                    return ReadFileAsStreamInternal(path);
                }

                throw;
            }
        }

        private static Stream ReadFileAsStreamInternal(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        private static async Task WriteToFile(string path, string content)
        {
            await using MemoryStream stream = new MemoryStream();
            await using StreamWriter writer = new StreamWriter(stream, Encoding.Default);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;
            await WriteToFile(path, stream);
        }

        private static async Task<(long ContentLength, DateTimeOffset LastModified)> WriteToFile(string path, Stream stream)
        {
            if (stream is not MemoryStream memStream)
            {
                memStream = new MemoryStream(); // lgtm [cs/local-not-disposed]
                await stream.CopyToAsync(memStream);
                memStream.Position = 0;
            }

            try
            {
                return await WriteToFileInternal(path, memStream);
            }
            catch (IOException ioException)
            {
                if (ioException.Message.Contains("used by another process"))
                {
                    await Task.Delay(400);
                    memStream.Position = 0;
                    return await WriteToFileInternal(path, memStream);
                }

                throw;
            }
            finally
            {
                await memStream.DisposeAsync();
            }
        }

        private static async Task<(long ContentLength, DateTimeOffset LastModified)> WriteToFileInternal(string path, MemoryStream stream)
        {
            long fileSize;
            await using (FileStream streamToWriteTo = File.Open(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                await stream.CopyToAsync(streamToWriteTo);
                streamToWriteTo.Flush();
                fileSize = streamToWriteTo.Length;
            }

            return (fileSize, DateTime.UtcNow);
        }

        public Task<Dictionary<string, List<DataElement>>> ReadAllForMultiple(List<string> instanceGuids)
        {
            throw new NotImplementedException();
        }
    }
}