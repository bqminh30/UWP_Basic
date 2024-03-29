﻿using Assignment_UWP.Config;
using Assignment_UWP.Empty;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Assignment_UWP.Service
{
    class AccountService
    {
        public static string TokenFileName = "sample.txt";
        ContentDialog contentDialog = new ContentDialog();
        public async Task<bool> RegisterAsync(Account account)
        {
            var jsonString = JsonConvert.SerializeObject(account);
            Console.WriteLine("Deserialize Object:");
            Console.WriteLine(jsonString);
            HttpClient httpClient = new HttpClient();
            HttpContent contentToSend = new StringContent(jsonString, Encoding.UTF8, ApiConfig.MediaType);
            var result = await httpClient.PostAsync($"{ApiConfig.ApiDomain}{ApiConfig.AccountPath}", contentToSend);
            if (result.StatusCode == System.Net.HttpStatusCode.Created)
            {
                //good case
                var content = await result.Content.ReadAsStringAsync();
                Account returnAccount = JsonConvert.DeserializeObject<Account>(content);
                Console.WriteLine(returnAccount.id);
                Console.WriteLine(returnAccount.firstName);
                return true;
            }
            else
            {
                //base case
                Console.WriteLine("Error 500");
                return false;
            }
          
        }
        public async Task<Credential> Login(LoginViewModal loginViewModel)
        {
            var jsonString = JsonConvert.SerializeObject(loginViewModel);
            using (HttpClient httpClient = new HttpClient())
            {
                HttpContent httpContent = new StringContent(jsonString, Encoding.UTF8, ApiConfig.MediaType);
                var result = await httpClient.PostAsync($"{ ApiConfig.ApiDomain}{ApiConfig.AccountLoginPath}", httpContent);
                var content = await result.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response login {content} - {result.StatusCode}");
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //good case
                    //var content = await result.Content.ReadAsStringAsync();
                    //Account returnAccount = JsonConvert.DeserializeObject<Account>(content);
                    SaveToken(content);
                    Credential credential = JsonConvert.DeserializeObject<Credential>(content);
                    return credential;
                }
                else
                {
                    //base case
                    Console.WriteLine("Error 500");
                }
            }
            return null;
        }

        private async void SaveToken(string content)
        {
            //goi den storage => lưu trong thu mục localFolder
            Windows.Storage.StorageFolder storageFolder =
            Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile sampleFile =
                await storageFolder.CreateFileAsync(TokenFileName,
                    Windows.Storage.CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(sampleFile, content);
        }

        // trong trường hơp lây dc thì thông tin account luu ra biến dùng chung ở App.xaml.cs
        public async Task<Account> GetLoggedInAccount()
        {
            Account account;
            Credential credential = await LoadToken();
            if (credential == null) // khong ton tai file token
            {
                return null;
            }
            account = await GetAccountInfomation(credential.access_token);
            return account;
        }


        // có token r check lại 1 lần nữa trên phía serve, lấy thông tin ng dùng về
        public async Task<Account> GetAccountInfomation(string token)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                // token
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                var result = await httpClient.GetAsync($"{ ApiConfig.ApiDomain}{ApiConfig.AccountPath}");
                var content = await result.Content.ReadAsStringAsync();
                Debug.WriteLine($"Response get account information {content} - {result.StatusCode}");
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    //good case
                    Account returnAccount = JsonConvert.DeserializeObject<Account>(content);
                    return returnAccount;
                }
                else
                {
                    //base case
                    contentDialog.Title = "Action fails";
                    contentDialog.Content = "Please try again";
                    contentDialog.CloseButtonText = "ok";
                    await contentDialog.ShowAsync();
                    return null;
                }
            }
        }

        // hàm load token, lấy csai file lưu trong LocalFolder ép về Credential
        private async Task<Credential> LoadToken()
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile storageFile = await storageFolder.GetFileAsync(TokenFileName);
                if (storageFile == null)
                {
                    return null;
                }
                string tokenString = await FileIO.ReadTextAsync(storageFile);
                Credential credential = JsonConvert.DeserializeObject<Credential>(tokenString);
                return credential;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }
}
