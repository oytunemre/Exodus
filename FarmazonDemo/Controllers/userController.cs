using FarmazonDemo.Data;
using FarmazonDemo.Models.Entities;
using FarmazonDemo.Models.Dto;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmazonDemo.Controllers
{

    //localhost:xxxxx/api/users
    [Route("api/[controller]")]
    [ApiController]
    public class userController : ControllerBase
    {
        private readonly ApplicationDbContext dbContext;
        public userController(ApplicationDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet]

        public IActionResult GetAllUsers()
        {
          var allUsers =  dbContext.Users.ToList();

            return Ok(allUsers);
        }


        [HttpGet]
        [Route("{id:guid")]
        public IActionResult GetUserbyId(Guid id)
        {

            var user = dbContext.Users.Find(id);

            if (user is null)
            {
                return NotFound();

            }
            else
            {
                return Ok(user);
            }
        }


            [HttpPost]

            public IActionResult AddUser(adduserDto addUserDto)
            {

                var userEntity = new Users()
                {
                    Email = addUserDto.Email,
                    Name = addUserDto.Name,
                    Password = addUserDto.Password,
                    Username = addUserDto.Username

                };


                dbContext.Add(userEntity);
                dbContext.SaveChanges();
                return Ok(userEntity);
            }
        }



    }

