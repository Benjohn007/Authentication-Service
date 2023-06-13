using AuthenticationServiceImp.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthenticationServiceImp.Service
{
    public interface IEmailService
    {
        void SendEmail(Message message);
    }
}
