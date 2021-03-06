﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DevAudit.AuditLibrary
{
    public class OSSIndexHttpClient
    {
        public string ApiVersion { get; set; }

        private String HOST = "https://ossindex.net/";
        //private String HOST = "http://localhost:8080/";

        public OSSIndexHttpClient(string api_version)
        {
            this.ApiVersion = api_version;
        }
                             
        public async Task<IEnumerable<OSSIndexArtifact>> SearchAsync(string package_manager, OSSIndexQueryObject package, Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@HOST);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
                HttpResponseMessage response = await client.GetAsync("v" + this.ApiVersion + "/search/artifact/" +
                    string.Format("{0}/{1}/{2}", package_manager, package.Name, package.Version, package.Vendor));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexArtifact> artifacts = JsonConvert.DeserializeObject<List<OSSIndexArtifact>>(r);
                    if (artifacts.Count() == 0 || transform == null)
                    {
                        return artifacts;
                    }
                    else
                    {
                        return transform(artifacts);
                    }
                }
                else
                {
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<IEnumerable<OSSIndexArtifact>> SearchAsync(string package_manager, IEnumerable<OSSIndexQueryObject> packages, 
            Func<List<OSSIndexArtifact>, List<OSSIndexArtifact>> transform)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@HOST);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
                HttpResponseMessage response = await client.PostAsync("v" + this.ApiVersion + "/search/artifact/" + package_manager,
                    new StringContent(JsonConvert.SerializeObject(packages),Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexArtifact> artifacts = JsonConvert.DeserializeObject<List<OSSIndexArtifact>>(r);
                    if (artifacts.Count() == 0 || transform == null)
                    {
                        return artifacts;
                    }
                    else
                    {
                        return transform(artifacts);
                    }
                }
                else
                {
                    throw new OSSIndexHttpException(package_manager, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<OSSIndexProject> GetProjectForIdAsync(string id)
        {
            if (string.IsNullOrEmpty(id))throw new ArgumentNullException("Project id.");
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@HOST);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
                HttpResponseMessage response = this.ApiVersion == "1.0" ? 
                    await client.GetAsync(string.Format("v" + this.ApiVersion + "/scm/{0}", id)) : await client.GetAsync(string.Format("v" + this.ApiVersion + "/project/{0}", id));
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<OSSIndexProject>>(r).FirstOrDefault();
                }
                else
                {
                    throw new OSSIndexHttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }
        }

        public async Task<List<OSSIndexPackageVulnerability>> GetPackageVulnerabilitiesAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException("Package id.");
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@HOST);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
                HttpResponseMessage response = await client.GetAsync(string.Format("v" + this.ApiVersion + "/package/{0}/vulnerabilities", id)); 
                    
                if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<OSSIndexPackageVulnerability>>(r);
                }
                else
                {
                    throw new OSSIndexHttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage);
                }
            }

        }

        public async Task<IEnumerable<OSSIndexProjectVulnerability>> GetVulnerabilitiesForIdAsync(string id)
        {
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(@HOST);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("user-agent", "DevAudit");
                HttpResponseMessage response = this.ApiVersion == "1.0" ?
                    await client.GetAsync(string.Format("v" + this.ApiVersion + "/scm/{0}/vulnerabilities", id)) : await client.GetAsync(string.Format("v" + this.ApiVersion + "/project/{0}/vulnerabilities", id)); if (response.IsSuccessStatusCode)
                {
                    string r = await response.Content.ReadAsStringAsync();
                    List<OSSIndexProjectVulnerability> result = JsonConvert.DeserializeObject<List<OSSIndexProjectVulnerability>>(r);
                    result.ForEach(v => 
                    {
                        v.ProjectId = id;
                        v.Title = HttpUtility.HtmlDecode(v.Title);
                        v.Summary = HttpUtility.HtmlDecode(v.Summary);
                    }); ;
                    
                    return result;
                }
                else
                {
                    throw new OSSIndexHttpException(id, response.StatusCode, response.ReasonPhrase, response.RequestMessage); 
                }
            }

        
        }
    }
}
