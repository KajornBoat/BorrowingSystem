using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using LockLock.Models;
using Newtonsoft.Json;

using System.IO;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using FirebaseAdmin.Auth;
using Firebase.Auth;

namespace LockLock.Controllers
{
    public class UserController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirestoreDb firestoreDb;

        private FirebaseAuthProvider auth;

        public UserController()
        {
            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);

            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));
        }

        public async Task<IActionResult> Index()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel user = documentSnapshot.ConvertTo<UserModel>();
                    user.UserID = uid;
                    return View(user);
                }
                catch
                {
                    Console.Write("Exception : ");
                    // Console.Write(ex);
                    return RedirectToAction("Index", "Home");
                }
            }
            else
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }

        }

        [HttpGet]
        public async Task<IActionResult> updateUser()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                    DocumentSnapshot documentSnapshot = await documentReference.GetSnapshotAsync();

                    UserModel user = documentSnapshot.ConvertTo<UserModel>();
                    return View(user);

                }
                catch (Exception ex)
                {
                    Console.Write("Exception : ");
                    Console.Write(ex);
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }
        }

        [HttpPost]
        public async Task<IActionResult> updateUser(UserModel user)
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                DocumentReference documentReference = firestoreDb.Collection("users").Document(uid);
                // await documentReference.SetAsync(user, SetOptions.Overwrite);
                await documentReference.UpdateAsync(new Dictionary<FieldPath, object>{
                    { new FieldPath("Firstname"), user.Firstname},
                    { new FieldPath("Lastname"), user.Lastname},
                    { new FieldPath("Tel"), user.Tel}
                });

                return RedirectToAction(nameof(Index));
            }
            else
            {
                Console.WriteLine("ID token must not be null or empty");
                return RedirectToAction("SignIn", "Account");
            }
        }
        [HttpGet]
        public async Task<IActionResult> changePassword()
        {
            string uid = await verifyTokenAsync();
            if (uid != null)
            {
                try
                {
                    UserRecord user = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.GetUserAsync(uid);
                    await auth.SendPasswordResetEmailAsync(user.Email);
                    return Ok();
                }
                catch
                {
                    return NotFound();
                }
            }
            else
            {
                Console.WriteLine("ID token must not be null or empty");
                return NotFound();
            }
        }

        private async Task<string> verifyTokenAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);

                return decodedToken.Uid;
            }
            catch
            {
                Console.Write("Exception : ");
                Console.WriteLine("ID token must not be null or empty");
                return null;
            }
        }
    }
}