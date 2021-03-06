﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Lykke.Common.Api.Contract.Responses;
using MAVN.Service.AdminManagement.Client;
using MAVN.Service.AdminManagement.Client.Models;
using MAVN.Service.AdminManagement.Client.Models.Requests;
using MAVN.Service.AdminManagement.Client.Models.Requests.Verification;
using MAVN.Service.AdminManagement.Client.Models.Responses.Verification;
using MAVN.Service.AdminManagement.Domain.Enums;
using MAVN.Service.AdminManagement.Domain.Exceptions;
using MAVN.Service.AdminManagement.Domain.Models;
using MAVN.Service.AdminManagement.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using AdminUser = MAVN.Service.AdminManagement.Client.Models.AdminUser;
using SuggestedValueType = MAVN.Service.AdminManagement.Client.Models.Enums.SuggestedValueType;

namespace MAVN.Service.AdminManagement.Controllers
{
    [Route("api/admins")]
    [ApiController]
    public class AdminsController : Controller, IAdminsClient
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IEmailVerificationService _emailVerificationService;
        private readonly IMapper _mapper;
        private readonly IAutofillValuesService _autofillValuesService;

        public AdminsController(
            IAdminUserService adminUserService,
            IEmailVerificationService emailVerificationService,
            IMapper mapper,
            IAutofillValuesService autofillValuesService)
        {
            _adminUserService = adminUserService;
            _emailVerificationService = emailVerificationService;
            _mapper = mapper;
            _autofillValuesService = autofillValuesService;
        }

        /// <summary>
        /// Returns a list of autofill values.
        /// </summary>
        /// <returns>A list of autofill values.</returns>
        [HttpGet("autofillValues")]
        [ProducesResponseType(typeof(AutofillValuesResponseModel), (int) HttpStatusCode.OK)]
        public async Task<AutofillValuesResponseModel> GetAutofillValuesAsync()
        {
            var values = await _autofillValuesService.GetAllAsync();

            return new AutofillValuesResponseModel
            {
                Values = values.Select(x => 
                    new SuggestedValueMapping
                    {
                        Type = _mapper.Map<SuggestedValueType>(x.Key),
                        Values = x.Value
                    })
                    .ToList()
            };
        }

        /// <summary>
        /// Registers new admin in the system.
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns><see cref="RegistrationResponseModel"/></returns>
        [HttpPost("register")]
        [ProducesResponseType(typeof(RegistrationResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<RegistrationResponseModel> RegisterAsync([FromBody] RegistrationRequestModel request)
        {
            var result = await _adminUserService.RegisterAsync(_mapper.Map<RegistrationRequestDto>(request));

            return _mapper.Map<RegistrationResponseModel>(result);
        }

        /// <summary>
        /// Confirm Email in the system.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <returns><see cref="VerificationCodeConfirmationResponseModel"/></returns>
        [HttpPost("confirmemail")]
        [ProducesResponseType(typeof(VerificationCodeConfirmationResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<VerificationCodeConfirmationResponseModel> ConfirmEmailAsync([FromBody] VerificationCodeConfirmationRequestModel request)
        {
            var confirmEmailModel = await _emailVerificationService.ConfirmCodeAsync(request.VerificationCode);

            return _mapper.Map<VerificationCodeConfirmationResponseModel>(confirmEmailModel);
        }

        /// <summary>
        /// Registers new admin in the system.
        /// </summary>
        /// <param name="adminRequest">Request</param>
        /// <returns><see cref="RegistrationResponseModel"/></returns>
        [HttpPost("update")]
        [ProducesResponseType(typeof(AdminUserResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<AdminUserResponseModel> UpdateAsync([FromBody] UpdateAdminRequestModel adminRequest)
        {
            var result = await _adminUserService.UpdateDataAsync(
                adminRequest.AdminUserId,
                adminRequest.Company,
                adminRequest.Department,
                adminRequest.FirstName,
                adminRequest.LastName,
                adminRequest.JobTitle,
                adminRequest.PhoneNumber,
                adminRequest.IsActive);

            return _mapper.Map<AdminUserResponseModel>(result);
        }

        /// <summary>
        /// Updates permissions for admin.
        /// </summary>
        /// <param name="request">Request</param>
        /// <returns><see cref="AdminUserResponseModel"/></returns>
        [HttpPost("updatePermissions")]
        [ProducesResponseType(typeof(AdminUserResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<AdminUserResponseModel> UpdatePermissionsAsync([FromBody] UpdatePermissionsRequestModel request)
        {
            var result = await _adminUserService.UpdatePermissionsAsync(
                request.AdminUserId,
                _mapper.Map<List<Permission>>(request.Permissions));

            return _mapper.Map<AdminUserResponseModel>(result);
        }

        /// <summary>
        /// Get admin users
        /// </summary>
        [HttpGet("getadminusers")]
        [ProducesResponseType(typeof(bool), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<IReadOnlyList<AdminUser>> GetAdminUsersAsync()
        {
            var adminUsers = await _adminUserService.GetAllAsync();
            return _mapper.Map<List<AdminUser>>(adminUsers.ToList());
        }

        /// <summary>
        /// Gets paginated list of AdminUsers
        /// </summary>
        /// <returns><see cref="PaginatedAdminUserResponseModel"/></returns>
        [HttpPost("paginated")]
        [ProducesResponseType(typeof(PaginatedAdminUserResponseModel), (int) HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int) HttpStatusCode.BadRequest)]
        public async Task<PaginatedAdminUserResponseModel> GetPaginatedAsync(
            [FromBody] PaginationRequestModel pagingInfo)
        {
            var result = await _adminUserService.GetPaginatedAsync(pagingInfo.CurrentPage, pagingInfo.PageSize, pagingInfo.Active);
            return _mapper.Map<PaginatedAdminUserResponseModel>(result);
        }

        /// <summary>
        /// Searches for the customer profile with a given email
        /// </summary>
        /// <returns><see cref="AdminUserResponseModel"/></returns>
        [HttpPost("getbyemail")]
        [ProducesResponseType(typeof(AdminUserResponseModel), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
        public async Task<AdminUserResponseModel> GetByEmailAsync([FromBody] GetByEmailRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.Email))
                throw new BadRequestException($"{nameof(request.Email)} can't be empty");

            var admin = await _adminUserService.GetByEmailAsync(request.Email, request.Active);
            
            return _mapper.Map<AdminUserResponseModel>(admin);
        }

        /// <summary>
        /// Gets admin by Id
        /// </summary>
        /// <returns><see cref="AdminUserResponseModel"/></returns>
        [HttpPost("getById")]
        [ProducesResponseType(typeof(AdminUserResponseModel), (int)HttpStatusCode.OK)]
        public async Task<AdminUserResponseModel> GetByIdAsync([FromBody] GetAdminByIdRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.AdminUserId))
                throw new BadRequestException($"{nameof(request.AdminUserId)} can't be empty");

            var admin = await _adminUserService.GetByIdAsync(request.AdminUserId);
            
            return _mapper.Map<AdminUserResponseModel>(admin);
        }

        /// <summary>
        /// Gets a list of admin permissions
        /// </summary>
        [HttpPost("getPermissions")]
        [ProducesResponseType(typeof(List<AdminPermission>), (int)HttpStatusCode.OK)]
        public async Task<List<AdminPermission>> GetPermissionsAsync([FromBody] GetAdminByIdRequestModel request)
        {
            if (string.IsNullOrWhiteSpace(request?.AdminUserId))
                throw new BadRequestException($"{nameof(request.AdminUserId)} can't be empty");

            var permissions = await _adminUserService.GetPermissionsAsync(request.AdminUserId);

            return _mapper.Map<List<AdminPermission>>(permissions);
        }

        /// <summary>
        /// Resets admin password
        /// </summary>
        [HttpPost("resetPassword")]
        [ProducesResponseType(typeof(ResetPasswordResponseModel), (int)HttpStatusCode.OK)]
        public async Task<ResetPasswordResponseModel> ResetPasswordAsync([FromBody] ResetPasswordRequestModel request)
        {
            var result = await _adminUserService.ResetPasswordAsync(request.AdminUserId, request.Password);

            return _mapper.Map<ResetPasswordResponseModel>(result);
        }
    }
}
