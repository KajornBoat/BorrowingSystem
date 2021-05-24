
using System;
using System.IO;
using System.Threading.Tasks;
using Firebase.Auth;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using LockLock.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace LockLock.Controllers
{
    public class AccountController : Controller
    {
        private string firebaseJSON = AppDomain.CurrentDomain.BaseDirectory + @"locklockconfigure.json";
        private FirebaseAuthProvider auth;
        private FirestoreDb firestoreDb;

        public AccountController()
        {
            auth = new FirebaseAuthProvider(
                            new FirebaseConfig("AIzaSyDYMUB0qohsGyFfdHCFWyxfcwr84HC-WCU"));

            string projectId;
            using (StreamReader r = new StreamReader(firebaseJSON))
            {
                string json = r.ReadToEnd();
                var myJObject = JObject.Parse(json);
                projectId = myJObject.SelectToken("project_id").Value<string>();
            }
            firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<IActionResult> IndexAsync()
        {
            try
            {
                var token = HttpContext.Session.GetString("_UserToken");
                FirebaseToken decodedToken;
                try
                {
                    decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                }
                catch (Exception ex)
                {
                    Console.Write("Exception : ");
                    Console.WriteLine(ex);
                    return RedirectToAction("SignIn", "Account");
                }

                return View();

            }
            catch (Exception ex)
            {
                Console.Write("Exception : ");
                Console.Write(ex);
                return View();
            }

        }

        public IActionResult SignUp()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignUp(SignUpModel singUpModel)
        {
            try
            {
                //create the user
                await auth.CreateUserWithEmailAndPasswordAsync(singUpModel.Email, singUpModel.Password, singUpModel.Firstname + " " + singUpModel.Lastname, true);
                //log in the new user
                var fbAuthLink = await auth
                                .SignInWithEmailAndPasswordAsync(singUpModel.Email, singUpModel.Password);
                string token = fbAuthLink.FirebaseToken;
                //saving the token in a session variable
                if (token != null)
                {
                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    singUpModel.role = "user";
                    await firestoreDb.Collection("users").Document(decodedToken.Uid).SetAsync(singUpModel);

                    HttpContext.Session.SetString("_UserToken", token);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.Write("Exception : ");
                Console.WriteLine(ex);

                ModelState.AddModelError(string.Empty, "EmailExists");
                return View();
            }
        }

        public IActionResult SignIn()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SignIn(SignInModel model)
        {
            try
            {
                var fbAuthLink = await auth
                                           .SignInWithEmailAndPasswordAsync(model.Email, model.Password);
                string token = fbAuthLink.FirebaseToken;
                //saving the token in a session variable
                if (token != null)
                {
                    HttpContext.Session.SetString("_UserToken", token);

                    FirebaseToken decodedToken = await FirebaseAdmin.Auth.FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(token);
                    DocumentReference userReference = firestoreDb.Collection("users").Document(decodedToken.Uid);
                    DocumentSnapshot userSnapshot = await userReference.GetSnapshotAsync();
                    UserModel user = userSnapshot.ConvertTo<UserModel>();

                    if (user.role == "user")
                    {
                        Console.WriteLine("User {0} Sign in.", decodedToken.Uid);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        Console.WriteLine("Admin {0} Sign in.", decodedToken.Uid);
                        return RedirectToAction("Index", "Admin");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                    return View();
                }
            }
            catch (Exception ex)
            {
                Console.Write("Exception : ");
                Console.Write(ex);
                ModelState.AddModelError(string.Empty, "Invalid username or password. Ex");
                return View();
            }
        }
        public IActionResult SignOut()
        {
            HttpContext.Session.Remove("_UserToken");
            return RedirectToAction("SignIn");
        }

    }
}