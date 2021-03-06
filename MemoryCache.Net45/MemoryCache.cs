﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Newtonsoft.Json;

namespace MemoryCache.Net
{
    public class MemoryCache : IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheEntry<dynamic>> _buffer;

        public MemoryCache()
        {
            _buffer = new ConcurrentDictionary<string, CacheEntry<dynamic>>();
        }

        public dynamic this[string key]
        {
            get => _buffer[key].Value;
            set
            {
                if (!_buffer.ContainsKey(key))
                    _buffer.TryAdd(key, new CacheEntry<dynamic>()
                    {
                        Serializable = ((Type)value.GetType()).IsSerializable,
                        Key = key,
                        Value = value
                    });
                else
                {
                    _buffer.TryUpdate(key, new CacheEntry<dynamic>
                    {
                        Serializable = ((Type)value.GetType()).IsSerializable,
                        Key = key,
                        Value = value
                    }, _buffer[key]);
                }
            }
        }

        public Dictionary<string, dynamic>.Enumerator GetEnumerator()
        {
            var buffer = _buffer
                .ToDictionary(entry => entry.Key, entry => entry.Value.Value);

            return buffer.GetEnumerator();
        }

        public void Write(string key, dynamic value)
        {
            _buffer[key] = new CacheEntry<dynamic>()
            {
                Serializable = ((Type)value.GetType()).IsSerializable,
                Key = key,
                Value = value
            };
        }

        public T Write<T>(string key, T value)
        {
            _buffer[key] = new CacheEntry<dynamic>()
            {
                Serializable = value.GetType().IsSerializable,
                Key = key,
                Value = value
            };

            return _buffer[key].Value;
        }

        public T Read<T>(string key)
        {
            return (T)Convert.ChangeType(_buffer[key].Value, typeof(T));
        }

        public void Delete(string key)
        {
            _buffer.TryRemove(key, out _);
        }

        public T Delete<T>(string key)
        {
            _buffer.TryRemove(key, out var entry);
            return entry.Value;
        }

        public T Save<T>()
        {
            var buffer = _buffer
                .Where(entry => entry.Value.Serializable)
                .ToDictionary(entry => entry.Key, entry => entry.Value.Value);

            var output = default(T);
            if (typeof(T) == typeof(string))
            {
                var json = JsonConvert.SerializeObject(buffer);
                output = (T)Convert.ChangeType(json, typeof(T));
            }

            if (typeof(T) == typeof(byte[]))
            {
                using (var stream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();

                    formatter.Serialize(stream, buffer);
                    output = (T) Convert.ChangeType(stream.ToArray(), typeof(T));
                }
            }

            return output;
        }

        public void Load<T>(T data, bool clear = true)
        {
            if (clear)
                _buffer.Clear();

            if (typeof(T) == typeof(string))
            {
                var json = (string)Convert.ChangeType(data, typeof(string));
                var buffer = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);

                foreach (var entry in buffer)
                    _buffer.TryAdd(entry.Key, new CacheEntry<dynamic>()
                    {
                        Key = entry.Key,
                        Value = entry.Value
                    });
            }

            if (typeof(T) == typeof(byte[]))
            {
                var binary = (byte[])Convert.ChangeType(data, typeof(byte[]));
                using (var stream = new MemoryStream(binary))
                {
                    var formatter = new BinaryFormatter();
                    var buffer = (Dictionary<string, dynamic>) formatter.Deserialize(stream);

                    foreach (var entry in buffer)
                        _buffer.TryAdd(entry.Key, new CacheEntry<dynamic>()
                        {
                            Key = entry.Key,
                            Value = entry.Value
                        });
                }
            }
        }

        public void Dispose()
        {
            _buffer.Clear();
        }
    }
}
