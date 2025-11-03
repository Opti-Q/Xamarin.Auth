//
//  Copyright 2013, Xamarin Inc.
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Security.Credentials;

namespace Xamarin.Auth
{
    internal partial class UwpAccountStore : AccountStore
    {
        private PasswordVault vault;

        public UwpAccountStore(char[] password = null)
        {
            try
            {
                vault = new PasswordVault();
            }
            catch (Exception ex)
            {
                // PasswordVault may not be available in some scenarios
                System.Diagnostics.Debug.WriteLine($"PasswordVault initialization failed: {ex.Message}");
            }
        }

        public override async Task<IEnumerable<Account>> FindAccountsForServiceAsync(string serviceId)
        {
            var accounts = new List<Account>();

            // Try loading from PasswordVault first (new storage method)
            if (vault != null)
            {
                try
                {
                    var credentials = vault.FindAllByResource(serviceId);
                    foreach (var credential in credentials)
                    {
                        try
                        {
                            credential.RetrievePassword();
                            var account = Account.Deserialize(credential.Password);
                            accounts.Add(account);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to load account from PasswordVault: {ex.Message}");
                        }
                    }

                    // If we found accounts in PasswordVault, return them
                    if (accounts.Count > 0)
                    {
                        return accounts;
                    }
                }
                catch (Exception ex)
                {
                    // No credentials found in PasswordVault, try legacy storage
                    System.Diagnostics.Debug.WriteLine($"PasswordVault lookup failed: {ex.Message}");
                }
            }

            // Fallback: Try loading from old encrypted file storage
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var files = await localFolder.GetFilesAsync().AsTask().ConfigureAwait(false);

                foreach (var file in files.Where(x => x.Name.StartsWith("xamarin.auth.") &&
                                                      x.Name.EndsWith("." + serviceId))
                                          .ToList())
                {
                    try
                    {
                        using (var stream = await file.OpenStreamForReadAsync().ConfigureAwait(false))
                        using (var reader = new BinaryReader(stream))
                        {
                            int length = reader.ReadInt32();
                            byte[] data = reader.ReadBytes(length);

                            byte[] unprot = (await DataProtectionExtensions.UnprotectAsync(data.AsBuffer()).ConfigureAwait(false)).ToArray();
                            var account = Account.Deserialize(Encoding.UTF8.GetString(unprot, 0, unprot.Length));
                            accounts.Add(account);

                            // Successfully decrypted old data - migrate to PasswordVault
                            if (vault != null)
                            {
                                try
                                {
                                    await SaveAsync(account, serviceId).ConfigureAwait(false);
                                    System.Diagnostics.Debug.WriteLine($"Migrated account to PasswordVault: {account.Username}");
                                }
                                catch (Exception migrationEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Failed to migrate account to PasswordVault: {migrationEx.Message}");
                                }
                            }

                            // Delete old encrypted file after successful migration
                            try
                            {
                                await file.DeleteAsync().AsTask().ConfigureAwait(false);
                            }
                            catch (Exception deleteEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Failed to delete old encrypted file: {deleteEx.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Failed to decrypt old data (e.g., due to Windows update breaking encryption)
                        System.Diagnostics.Debug.WriteLine($"Failed to decrypt legacy account file {file.Name}: {ex.Message}, HResult: {ex.HResult:X}");

                        // Try to delete corrupted file
                        try
                        {
                            await file.DeleteAsync().AsTask().ConfigureAwait(false);
                            System.Diagnostics.Debug.WriteLine($"Deleted corrupted file: {file.Name}");
                        }
                        catch
                        {
                            // Ignore deletion errors
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to access legacy storage: {ex.Message}");
            }

            return accounts;
        }

        public override async Task DeleteAsync(Account account, string serviceId)
        {
            // Delete from PasswordVault
            if (vault != null && !string.IsNullOrEmpty(account.Username))
            {
                try
                {
                    var credential = vault.Retrieve(serviceId, account.Username);
                    vault.Remove(credential);
                    System.Diagnostics.Debug.WriteLine($"Deleted account from PasswordVault: {account.Username}");
                }
                catch (Exception ex)
                {
                    // Credential might not exist in vault
                    System.Diagnostics.Debug.WriteLine($"Failed to delete from PasswordVault: {ex.Message}");
                }
            }

            // Also try to delete from old file-based storage for backward compatibility
            var path = GetAccountPath(account, serviceId);
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.GetFileAsync(path).AsTask().ConfigureAwait(false);
                await file.DeleteAsync().AsTask().ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Deleted legacy file: {path}");
            }
            catch
            {
                // Ignore this error if file doesn't exist
            }
        }

        public override async Task SaveAsync(Account account, string serviceId)
        {
            if (vault == null)
            {
                throw new InvalidOperationException("PasswordVault is not available on this system");
            }

            if (string.IsNullOrEmpty(account.Username))
            {
                throw new ArgumentException("Account must have a username", nameof(account));
            }

            try
            {
                // Remove existing credential if it exists
                try
                {
                    var existingCredential = vault.Retrieve(serviceId, account.Username);
                    vault.Remove(existingCredential);
                }
                catch
                {
                    // Credential doesn't exist yet, which is fine
                }

                // Save to PasswordVault with serialized account as password
                var serializedAccount = account.Serialize();
                var credential = new PasswordCredential(serviceId, account.Username, serializedAccount);
                vault.Add(credential);

                System.Diagnostics.Debug.WriteLine($"Saved account to PasswordVault: {account.Username}");

                // Clean up any old encrypted file for this account
                var path = GetAccountPath(account, serviceId);
                try
                {
                    var localFolder = ApplicationData.Current.LocalFolder;
                    var file = await localFolder.GetFileAsync(path).AsTask().ConfigureAwait(false);
                    await file.DeleteAsync().AsTask().ConfigureAwait(false);
                    System.Diagnostics.Debug.WriteLine($"Cleaned up old file: {path}");
                }
                catch
                {
                    // Old file doesn't exist, which is fine
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save account to PasswordVault: {ex.Message}, HResult: {ex.HResult:X}");
                throw;
            }
        }

        private static string GetAccountPath(Account account, string serviceId)
        {
            return String.Format("xamarin.auth.{0}.{1}", account.Username, serviceId);
        }
    }
}
