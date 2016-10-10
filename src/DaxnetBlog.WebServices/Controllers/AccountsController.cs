﻿// ===========================================================================================================
//      _                                 _              _       _                 
//     | |                               | |            | |     | |                
//   __| |   __ _  __  __  _ __     ___  | |_   ______  | |__   | |   ___     __ _ 
//  / _` |  / _` | \ \/ / | '_ \   / _ \ | __| |______| | '_ \  | |  / _ \   / _` |
// | (_| | | (_| |  >  <  | | | | |  __/ | |_           | |_) | | | | (_) | | (_| |
//  \__,_|  \__,_| /_/\_\ |_| |_|  \___|  \__|          |_.__/  |_|  \___/   \__, |
//                                                                            __/ |
//                                                                           |___/ 
//
// 
// Daxnet Personal Blog
// Copyright © 2016 by daxnet (Sunny Chen)
//
// https://github.com/daxnet/daxnet-blog
//
// MIT License
// 
// Copyright(c) 2016 Sunny Chen
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
// ===========================================================================================================

using DaxnetBlog.Common;
using DaxnetBlog.Common.Storage;
using DaxnetBlog.Domain.Model;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace DaxnetBlog.WebServices.Controllers
{
    /// <summary>
    /// Represents the API controller that provides account accessibility features.
    /// </summary>
    [Route("api/[controller]")]
    public class AccountsController : Controller
    {
        private readonly IStorage storage;
        private readonly IEntityStore<Account, int> accountStore;

        /// <summary>
        /// Initializes a new instance of <c>AccountsController</c> class.
        /// </summary>
        /// <param name="storage"></param>
        /// <param name="accountStore"></param>
        public AccountsController(IStorage storage, IEntityStore<Account, int> accountStore)
        {
            this.storage = storage;
            this.accountStore = accountStore;
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> CreateAccount([FromBody] dynamic accountObject)
        {
            var userName = (string)accountObject.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "userName cannot be null.");
            }
            var password = (string)accountObject.Password;
            if (string.IsNullOrEmpty(password))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "password cannot be null.");
            }

            var passwordHash = Crypto.ComputeHash(password, userName);

            var email = (string)accountObject.Email;
            if (string.IsNullOrEmpty(email))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "email cannot be null.");
            }
            var nickName = (string)accountObject.NickName;
            if (string.IsNullOrEmpty(nickName))
            {
                nickName = userName;
            }

            var result = await storage.ExecuteAsync(async(connection, transaction, cancellationToken) => 
            {
                var userWithName = (await accountStore.SelectAsync(connection, 
                    x => x.UserName == userName, 
                    transaction: transaction, 
                    cancellationToken: cancellationToken)).FirstOrDefault();
                if (userWithName != null)
                {
                    throw new ServiceException(HttpStatusCode.Conflict, $"用户名 {userName} 已经存在。");
                }

                var userWithEmail = (await accountStore.SelectAsync(connection,
                    x => x.EmailAddress == email,
                    transaction: transaction,
                    cancellationToken: cancellationToken)).FirstOrDefault();
                if (userWithEmail != null)
                {
                    throw new ServiceException(HttpStatusCode.Conflict, $"电子邮件地址 {email} 已经存在。");
                }

                var rowsAffected = await accountStore.InsertAsync(
                   new Account
                   {
                       UserName = userName,
                       PasswordHash = passwordHash,
                       EmailAddress = email,
                       NickName = nickName,
                       DateRegistered = DateTime.UtcNow
                   },
                   connection,
                   new Expression<Func<Account, object>>[] { a => a.Id },
                   transaction, cancellationToken);

                if (rowsAffected > 0)
                {
                    var insertedAccount = (await accountStore.SelectAsync(connection, 
                        x => x.UserName == userName,
                        new Sort<Account, int> { { x=>x.DateRegistered, SortOrder.Descending } }, // Gets the last record inserted, if any duplicates
                        transaction: transaction, 
                        cancellationToken: cancellationToken)).FirstOrDefault();

                    if (insertedAccount == null)
                    {
                        throw new ServiceException("No account was created in the current transaction.");
                    }
                    return insertedAccount.Id;
                }
                return 0;
            });

            var uri = Url.Action("GetById", new { id = result });
            return Created(uri, result);
        }

        [HttpGet]
        [Route("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var account = (await storage.ExecuteAsync(async (connection, cancellationToken) =>
                await accountStore.SelectAsync(connection, x => x.Id == id, cancellationToken: cancellationToken))).FirstOrDefault();

            if (account == null)
            {
                throw new ServiceException(HttpStatusCode.NotFound, $"No account was found with the id of {id}.");
            }

            return Ok(new {
                account.Id,
                account.UserName,
                account.NickName,
                account.EmailAddress,
                account.DateRegistered,
                account.DateLastLogin
            });
        }

        [HttpGet]
        [Route("name/{name}")]
        public async Task<IActionResult> GetByUserName(string name)
        {
            var account = (await storage.ExecuteAsync(async (connection, cancellationToken) =>
                await accountStore.SelectAsync(connection, x => x.UserName == name, cancellationToken: cancellationToken))).FirstOrDefault();

            if (account == null)
            {
                throw new ServiceException(HttpStatusCode.NotFound, $"No account was found with the userName of {name}.");
            }

            return Ok(new
            {
                account.Id,
                account.UserName,
                account.NickName,
                account.EmailAddress,
                account.DateRegistered,
                account.DateLastLogin
            });
        }

        [HttpGet]
        [Route("authenticate/passwordhash/{id}")]
        public async Task<IActionResult> GetPasswordHash(int id)
        {
            var account = (await storage.ExecuteAsync(async (connection, cancellationToken) =>
                await accountStore.SelectAsync(connection, x => x.Id == id, cancellationToken: cancellationToken))).FirstOrDefault();

            if (account == null)
            {
                throw new ServiceException(HttpStatusCode.NotFound, $"No account was found with the id of {id}.");
            }

            return Ok(account.PasswordHash);
        }

        [HttpPost]
        [Route("authenticate/{id}")]
        public async Task<IActionResult> Authenticate(int id, [FromBody] dynamic passwordModel)
        {
            var password = (string)passwordModel.Password;
            if (string.IsNullOrEmpty(password))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, "The password argument cannot be null.");
            }

            var account = (await storage.ExecuteAsync(async (connection, cancellationToken) =>
                await accountStore.SelectAsync(connection, x => x.Id == id, cancellationToken: cancellationToken))).FirstOrDefault();

            if (account == null)
            {
                throw new ServiceException(HttpStatusCode.NotFound, $"No account was found with the id of {id}.");
            }

            return Ok(account.ValidatePassword(password));
        }

        [HttpPost]
        [Route("verification/create")]
        public async Task<IActionResult> GenerateEmailVerificationCodeByUserName([FromBody] dynamic userNameModel)
        {
            var userName = (string)userNameModel.UserName;
            if (string.IsNullOrEmpty(userName))
            {
                throw new ServiceException(HttpStatusCode.BadRequest, $"{nameof(userName)}参数不能为空。");
            }

            var verificationCode = Utils.GetUniqueStringValue(16);
            return await storage.ExecuteAsync(async (connection, transaction, cancellationToken) =>
            {
                var account = (await accountStore.SelectAsync(connection, 
                    x => x.UserName == userName, 
                    transaction: transaction, 
                    cancellationToken: cancellationToken)).FirstOrDefault();

                if (account == null)
                {
                    throw new ServiceException(HttpStatusCode.NotFound, $"用户{userName}不存在。");
                }

                account.EmailVerifyCode = verificationCode;

                var rowsAffected = await accountStore.UpdateAsync(account,
                    connection,
                    expr => expr.Id == account.Id,
                    new Expression<Func<Account, object>>[] { x => x.EmailVerifyCode },
                    transaction);

                if (rowsAffected > 0)
                    return Ok(new
                    {
                        UserName = userName,
                        VerificationCode = verificationCode
                    });
                throw new ServiceException("生成电子邮件验证码失败。");
            });
        }
    }
}
