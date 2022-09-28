﻿using CodeM.FastApi.Context;
using System.Threading.Tasks;

namespace CodeM.FastApi.Controllers
{
    public class HelloController : BaseController
    {

        public async Task Handle(ControllerContext cc)
        {
            dynamic helloService = Service("Hello");
            string hi = helloService.GetHi();
            await cc.JsonAsync(hi);
        }

        /// <summary>
        /// 可通过传入版本号v2参数，测试版本控制功能
        /// </summary>
        /// <param name="cc"></param>
        /// <returns></returns>
        public async Task Handle_v2(ControllerContext cc)
        {
            dynamic helloService = Service("Hello");
            string hi = helloService.GetHi();
            await cc.JsonAsync(hi + "这是v2版本返回的。");
        }
    }
}
