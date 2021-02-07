using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TekTox.Core.Services;
using TekTox.DAL;

namespace TekTox.Bot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var SqlServerConnectionString = Configuration["connectionstring"];

            services.AddDbContext<RPGContext>(options =>
            {
                options.UseSqlServer(SqlServerConnectionString,
                   x => x.MigrationsAssembly("TekTox.DAL.Migrations"));
            });

            services.AddScoped<IEventListService, EventListService>();
            services.AddRazorPages();

            var serviceProvider = services.BuildServiceProvider();

            var bot = new Bot(serviceProvider, Configuration);
            services.AddSingleton(bot);
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
