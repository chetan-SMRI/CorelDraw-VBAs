using System;
using System.Collections.Generic;
using Corel.Interop.VGCore;

namespace LicenseOverlap.DataSource
{
    public class DataSourceFactory : ICUIDataSourceFactory
    {
        private readonly Dictionary<string, Type> dataSources = new Dictionary<string, Type>();

        public void AddDataSource(string name, Type dataSource)
        {
            dataSources.Add(name, dataSource);
        }

        public void Register()
        {
            foreach (string name in dataSources.Keys)
            {
                ControlUI.CorelApp.FrameWork.Application.RegisterDataSource(name, this);
            }
        }

        public void CreateDataSource(string dataSourceName, DataSourceProxy proxy, out object value)
        {
            value = CreateDataSource(dataSourceName, proxy);
        }

        public object CreateDataSource(string dataSourceName, DataSourceProxy proxy)
        {
            if (!dataSources.ContainsKey(dataSourceName))
            {
                return null;
            }

            Type type = dataSources[dataSourceName];
            return type.Assembly.CreateInstance(
                type.FullName,
                true,
                System.Reflection.BindingFlags.CreateInstance,
                null,
                new object[] { proxy },
                null,
                null);
        }
    }
}
