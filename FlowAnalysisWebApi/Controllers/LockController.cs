using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Newtonsoft.Json;
using FlowAnalysisWebApi.Models;
using Renci.SshNet;
using System.Diagnostics;

namespace LineWebApi.Controllers
{
    public class LockController : ApiController
    {
        public void Post([FromBody]string value)
        {
            string _host = "172.16.62.56";
            string _username = "user";
            string _password = "p@ssw0rd";
            int _port = 22;
            try
            {
                ConnectionInfo conInfo = new ConnectionInfo(_host, _port, _username, new AuthenticationMethod[]{
                new PasswordAuthenticationMethod(_username,_password)});
                dynamic JSONData = JsonConvert.DeserializeObject(value);
                string AdminAction = JSONData["Action"];
                if (AdminAction == "Lock")
                {
                    List<string> IPList = JsonConvert.DeserializeObject<List<string>>((string)JSONData["IP"]);
                    using (FlowDatas db = new FlowDatas())
                    {
                        List<string> DbLockIPs = db.LockUsers.Select(c => c.IP)
                            .Union(db.WhiteUsers.Select(c => c.IP)).ToList();
                        List<string> LockIP = IPList.Where(c => !DbLockIPs.Contains(c)).ToList();

                        using (SshClient sshClient = new SshClient(conInfo))
                        {
                            if (!sshClient.IsConnected)
                            {
                                //連線
                                sshClient.Connect();
                            }

                            foreach (var q in LockIP)
                            {
                                SshCommand sshCmd = sshClient.RunCommand("ip firewall address-list add address=" + q +" list=lockip");
                                if (!string.IsNullOrWhiteSpace(sshCmd.Error))
                                {
                                    Debug.WriteLine(sshCmd.Error);
                                }
                                else
                                {
                                    db.LockUsers.Add(new FlowDatas.LockTable { Id = Guid.NewGuid(), IP = q, Time = DateTime.Now, Reason = "惡意行為" });
                                    Debug.WriteLine(sshCmd.Result);
                                }
                            }
                            if (LockIP.Count > 0)
                                db.SaveChanges();
                        }
                    }
                }
                else if (AdminAction == "UnLock")
                {
                    string IP = (string)JSONData["IP"];

                    using (FlowDatas db = new FlowDatas())
                        if (db.LockUsers.Where(c => c.IP == IP).Count() > 0)
                        {
                            using (SshClient sshClient = new SshClient(conInfo))
                            {
                                if (!sshClient.IsConnected)
                                {
                                    //連線
                                    sshClient.Connect();
                                }


                                SshCommand sshCmd = sshClient.RunCommand("/ip firewall address-list remove [find list=lockip  address ="+ IP +"]");
                                if (!string.IsNullOrWhiteSpace(sshCmd.Error))
                                {
                                    Debug.WriteLine(sshCmd.Error);
                                }
                                else
                                {
                                    db.LockUsers.RemoveRange(db.LockUsers.Where(c => c.IP == IP).ToArray());
                                    db.SaveChanges();
                                    Debug.WriteLine(sshCmd.Result);
                                }
                            }
                        }
                }

            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}