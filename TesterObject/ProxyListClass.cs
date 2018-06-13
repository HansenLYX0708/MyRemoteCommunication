using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitachi.Tester.Module
{
    /// <summary>
    /// 
    /// </summary>
    public class ProxyListClass
    {
        #region Fields
        /// <summary>
        /// List of proxy structs
        /// </summary>
        private List<ProxyStruct> m_proxyList;
        #endregion Fields

        #region Constructors
        public ProxyListClass()
        {
            m_proxyList = new List<ProxyStruct>();
        }
        #endregion Constructors

        #region Properties

        #endregion Properties

        #region Methods
        /// <summary>
        /// Get proxy item via numeric index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ITesterObjectCallback Get(int index)
        {
            return m_proxyList[index].m_proxy;
        }

        /// <summary>
        /// Get proxy with key.
        /// </summary>
        /// <param name="_computerName"></param>
        /// <param name="_userName"></param>
        /// <returns></returns>
        public ITesterObjectCallback Get(string _computerName, string _userName)
        {
            for (int i = 0; i < m_proxyList.Count; i++)
            {
                ProxyStruct aProxy = m_proxyList[i];
                if ((aProxy.m_computerName == _computerName) &&
                    (aProxy.UserName == _userName))
                {
                    return aProxy.m_proxy;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a new one.
        /// If already here does not add duplicate.
        /// </summary>
        /// <param name="newProxy"></param>
        public void Add(ProxyStruct newProxy)
        {
            for (int i = 0; i < m_proxyList.Count; i++)
            {
                ProxyStruct aProxy = m_proxyList[i];
                if ((aProxy.m_computerName == newProxy.m_computerName) &&
                    (aProxy.UserName == newProxy.UserName))
                {
                    return;
                }
            }
            m_proxyList.Add(newProxy);
        }

        /// <summary>
        /// RemoveAt
        /// </summary>
        /// <param name="index"></param>
        public void Remove(int index)
        {
            if (index < m_proxyList.Count && index >= 0)
            {
                m_proxyList.RemoveAt(index);
            }
        }

        /// <summary>
        /// Remove via key.
        /// </summary>
        /// <param name="_computerName"></param>
        public void Remove(string _computerName, string _UserName)
        {
            for (int i = 0; i < m_proxyList.Count; i++)
            {
                ProxyStruct aProxy = m_proxyList[i];
                if ((aProxy.m_computerName == _computerName) &&
                    (aProxy.UserName == _UserName))
                {
                    m_proxyList.RemoveAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Count of callback proxies in our list.
        /// </summary>
        public int Count
        {
            get
            {
                return m_proxyList.Count;
            }
        }
        #endregion Methods
    }  // end class

    /// <summary>
    /// Struct to store callback proxy and ID from whence it came.
    /// </summary>
    public class ProxyStruct
    {
        #region Fields
        public string m_computerName;
        public ITesterObjectCallback m_proxy;
        public string UserName;
        #endregion Fields

        #region Constructors
        public ProxyStruct()
        {
            m_computerName = "";
            m_proxy = null;
            UserName = "";
        }

        public ProxyStruct(string _computerName, string _UserName, ITesterObjectCallback _proxy)
        {
            m_computerName = _computerName;
            m_proxy = _proxy;
            UserName = _UserName;
        }
        #endregion Constructors
    }  // end class
}
