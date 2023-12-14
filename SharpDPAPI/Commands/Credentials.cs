﻿using System;
using System.Collections.Generic;
using System.IO;

namespace SharpDPAPI.Commands
{
    public class Credentials : ICommand
    {
        public static string CommandName => "credentials";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\r\n[*] Action: User DPAPI Credential Triage\r\n");
            arguments.Remove("credentials");

            Dictionary<string, string> masterkeys = new Dictionary<string, string>();
            string server = "";             // used for remote server specification

            if (arguments.ContainsKey("/server"))
            {
                server = arguments["/server"];
                Console.WriteLine("[*] Triaging remote server: {0}\r\n", server);
            }

            // {GUID}:SHA1 keys are the only ones that don't start with /
            
            foreach (KeyValuePair<string, string> entry in arguments)
            {
                if (!entry.Key.StartsWith("/"))
                {
                    masterkeys.Add(entry.Key, entry.Value);
                }
            }

            if (arguments.ContainsKey("/pvk"))
            {
                // use a domain DPAPI backup key to triage masterkeys
                masterkeys = SharpDPAPI.Dpapi.PVKTriage(arguments);
            }
            else if (arguments.ContainsKey("/mkfile"))
            {
                masterkeys = SharpDPAPI.Helpers.ParseMasterKeyFile(arguments["/mkfile"]);
            }
            else if (arguments.ContainsKey("/password"))
            {
                Console.WriteLine("[*] Will decrypt user masterkeys with password: {0}\r\n", arguments["/password"]);
                masterkeys = Triage.TriageUserMasterKeys(show: true, computerName: server, password: arguments["/password"]);
            }
            else if (arguments.ContainsKey("/ntlm"))
            {
                Console.WriteLine("[*] Will decrypt user masterkeys with NTLM hash: {0}\r\n", arguments["/ntlm"]);
                masterkeys = Triage.TriageUserMasterKeys(show: true, computerName: server, ntlm: arguments["/ntlm"]);
            }
            else if (arguments.ContainsKey("/prekey"))
            {
                Console.WriteLine("[*] Will decrypt user masterkeys with PreKey: {0}\r\n", arguments["/prekey"]);
                masterkeys = Triage.TriageUserMasterKeys(show: true, computerName: server, prekey: arguments["/prekey"]);
            }
            else if (arguments.ContainsKey("/rpc"))
            {
                Console.WriteLine("[*] Will ask a domain controller to decrypt masterkeys for us\r\n");
                masterkeys = Triage.TriageUserMasterKeys(show: true, rpc: true);
            }

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');

                if (File.Exists(target))
                {
                    Console.WriteLine("[*] Target Credential File: {0}\r\n", target);
                    Triage.TriageCredFile(target, masterkeys);
                }
                else if (Directory.Exists(target))
                {
                    Console.WriteLine("[*] Target Credential Folder: {0}\r\n", target);
                    Triage.TriageCredFolder(target, masterkeys);
                }
                else
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid file or directory.", target);
                }
            }
            else
            {
                if (arguments.ContainsKey("/server") && !arguments.ContainsKey("/pvk") && !arguments.ContainsKey("/password"))
                {
                    Console.WriteLine("[X] The '/server:X' argument must be used with '/pvk:BASE64...' or '/password:X' !");
                }
                else
                {
                    Triage.TriageUserCreds(masterkeys, server);
                }
            }
        }
    }
}