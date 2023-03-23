using AutoMapper;
using BusinessObject.Models;
using BusinessObject.Res;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Mapper
{
    public class AccountMapper : Profile
    {
        public AccountMapper() {
            CreateMap<Account, AccRes>();
        }
    }
}
