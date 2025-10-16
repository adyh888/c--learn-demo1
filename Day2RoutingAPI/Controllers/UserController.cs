using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        //模拟数据库
        private static List<User> _user = new List<User>
        {
            new User{Id=1,Name="张三",Age=18,Email = "zhang@example.com"},
            new User{Id=2,Name="李四",Age=19,Email = "zhang@example.com"},
            new User{Id=3,Name="王五",Age=20,Email = "zhang@example.com"},
            new User{Id=4,Name="赵六",Age=21,Email = "zhang@example.com"},
            new User{Id=5,Name="田七",Age=22,Email = "zhang@example.com"},
        };
        
        //获取所有用户
        [HttpGet]
        public IActionResult GetAllUsers()
        {
            return Ok(_user);
        }
        
        //获取单个用户
        [HttpGet("{id}")]
        public IActionResult GetUserById(int id)
        {
            var user = _user.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new {error="用户不存在"});
                
            }
            return Ok(user);
        }
        
        //创建用户
        [HttpPost]
        public IActionResult CreateUser([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newUser = new User
            {
                Id = _user.Max(u => u.Id) + 1,
                Name = dto.Name,
                Age = dto.Age,
                Email = dto.Email
            };
                _user.Add(newUser);

                return CreatedAtAction(nameof(GetUserById), new { id = newUser.Id }, newUser);
        }
        
        //更新用户
        [HttpPut("{id}")]
        public IActionResult UpdateUser(int id, [FromBody] UpdateUserDto dto)
        {
            var user = _user.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new {error=$"User with id {id} not found"});
            }
            
            //更新字段
            if(!string.IsNullOrEmpty(dto.Name))
            {
               user.Name = dto.Name;
            }

            if (!string.IsNullOrEmpty(dto.Email))
            {
                user.Email = dto.Email;
            }
            if(dto.Age.HasValue)
            {
                user.Age = dto.Age.Value;
            }
            return Ok(user);
            
        }
        
        //删除用户
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _user.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(new {error=$"User with id {id} not found"});
            }
            _user.Remove(user);
            return NoContent();
        }
        
        
        //分页查询
        [HttpGet("paged")]
        public IActionResult GetPagedUsers([FromQuery] int page=1,[FromQuery] int pageSize=10)
        {
            if(page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { error = "Page and PageSize must be greater than 0" });
            }

            var skip = (page - 1) * pageSize;
            var users=  _user.Skip(skip).Take(pageSize);
            //打印skip数据和users数据
            Console.WriteLine($"Skip: {skip}, Users Count: {users.Count()}");
            
            return Ok(new
            {
                page = page,
                pageSize = pageSize,
                total = _user.Count,
                data = users
            });
        }
        
        //搜索用户
        [HttpGet("search")]
        public IActionResult SearchUsers([FromQuery] string keyword)
        {
            if(string.IsNullOrEmpty(keyword))
            {
                return BadRequest(new { error = "Keyword is required" });
            }
            var results = _user.Where(u => u.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) || u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            return Ok(results);
        }
        
        //用户模型
        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Email { get; set; }
        }
        //创建用户DTO
        public class CreateUserDto
        {
            [Required(ErrorMessage = "Name is required")]
            [StringLength(100, MinimumLength = 2,ErrorMessage = "Name length must be between 2 and 100 characters")]
            public string Name { get; set; }
            
            [Range(0,150,ErrorMessage = "Age must be between 0 and 150")]
            public int Age { get; set; }
            
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }
        }
        
        //更新用户DTO
        public class UpdateUserDto
        {
            [StringLength(100, MinimumLength = 2,ErrorMessage = "Name length must be between 2 and 100 characters")]
            public string Name { get; set; }
            
            [Range(0,150,ErrorMessage = "Age must be between 0 and 150")]
            public int? Age { get; set; }
            
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }
        }
    }
}
