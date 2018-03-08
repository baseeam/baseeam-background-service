/*******************************************************
 * Copyright 2016 (C) BaseEAM Systems, Inc
 * All Rights Reserved
*******************************************************/
using BaseEAM.Core;
using BaseEAM.Core.Data;
using BaseEAM.Core.Domain;
using System.Linq;

namespace BaseEAM.BackgroundService
{
    public class BackgroundServiceWorkContext : IWorkContext
    {
        private readonly IRepository<User> _userRepository;
        private User _cachedUser;

        public BackgroundServiceWorkContext(IRepository<User> userRepository)
        {
            this._userRepository = userRepository;
        }

        public User CurrentUser
        {
            get
            {
                if (_cachedUser != null)
                    return _cachedUser;

                var user = _userRepository.GetAll().First(u => u.Name == SystemUserNames.BackgroundTask && u.IsSystemAccount == true);
                _cachedUser = user;
                return _cachedUser;
            }
            set
            {
                _cachedUser = value;
            }
        }

        public Currency WorkingCurrency { get; set; }

        public Language WorkingLanguage { get; set; }
    }
}
