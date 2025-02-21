﻿using System;
using System.Linq;
using System.Reflection;
using System.Data.SqlClient;
using System.IO;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public class SqlCeDatabaseFactory
    {
        private static string FrameworkPath = "net48\\";
        public static ISqlCeDatabase Create(string connectionString)
        {
            var type = GetImplementation(connectionString);
            return Activator.CreateInstance(type, connectionString) as ISqlCeDatabase;
        }

        public static ISqlCeDatabase Create(string defaultNamespace, string connectionString)
        {
            var type = GetImplementation(connectionString);
            return  Activator.CreateInstance(type, defaultNamespace, connectionString) as ISqlCeDatabase;
        }

        public static ISqlCeDatabase Create(SupportedVersions version)
        {
            var type = GetImplementation(version);
            return Activator.CreateInstance(type) as ISqlCeDatabase;
        }

        private static Type GetImplementation(string connectionString)
        {
            var file = new SqlConnectionStringBuilder(connectionString).DataSource;
            var version = GetVersion(file);

            return GetImplementation(version);
        }

        private static Type GetImplementation(SupportedVersions version)
        {
            switch (version)
            {
                case SupportedVersions.SqlCe31:
                    return LoadSqlCe31();
                case SupportedVersions.SqlCe35:
                    return LoadSqlCe35();
                case SupportedVersions.SqlCe40:
                    return LoadSqlCe40();
                default:
                    throw new NotSupportedException();
            }
        }

        private static string GetExecutingAssemblyPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static Type LoadSqlCe31()
        {
            var file = Path.Combine(Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe31"), FrameworkPath), "SqlCeDatabase31.dll");
            if (!File.Exists(file)) file = Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe31"), "SqlCeDatabase31.dll");
            if (!File.Exists(file)) file = Path.Combine(GetExecutingAssemblyPath(), "SqlCeDatabase31.dll");

            var assembly = Assembly.LoadFrom(file);
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        private static Type LoadSqlCe35()
        {
            var file = Path.Combine(Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe35"), FrameworkPath), "SqlCeDatabase35.dll");
            if (!File.Exists(file)) file = Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe35"), "SqlCeDatabase35.dll");
            if (!File.Exists(file)) file = Path.Combine(GetExecutingAssemblyPath(), "SqlCeDatabase35.dll");

            var assembly = Assembly.LoadFrom(file);
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        private static Type LoadSqlCe40()
        {
            var file = Path.Combine(Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe40"), FrameworkPath), "SqlCeDatabase40.dll");
            if (!File.Exists(file)) file = Path.Combine(Path.Combine(GetExecutingAssemblyPath(), "SqlCe40"), "SqlCeDatabase40.dll");
            if (!File.Exists(file)) file = Path.Combine(GetExecutingAssemblyPath(), "SqlCeDatabase40.dll");


            var assembly = Assembly.LoadFrom(file);
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        

        private static SupportedVersions GetVersion(string file)
        {
            using (var fs = new FileStream(file, FileMode.Open))
            {
                fs.Seek(16, SeekOrigin.Begin);
                var reader = new BinaryReader(fs);
                var signature = reader.ReadInt32();

                switch (signature)
                {
                    case 0x73616261: return SupportedVersions.SqlCe20;
                    case 0x002dd714: return SupportedVersions.SqlCe31;
                    case 0x00357b9d: return SupportedVersions.SqlCe35;
                    case 0x003D0900: return SupportedVersions.SqlCe40;
                    default: return SupportedVersions.Unsupported;
                }
            }
        }
    }

    public enum SupportedVersions
    {
        Unsupported,
        SqlCe20,
        SqlCe31,
        SqlCe35,
        SqlCe40
    }
}
