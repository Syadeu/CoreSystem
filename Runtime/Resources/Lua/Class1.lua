--local Main = require 'Main'
--CoreSystem.Log('Test')
--function tester()
	-- body
	--CoreSystem.Log('success')
--end

--function testValues(a, b)
	-- body
	--CoreSystem.Log(a)
	--CoreSystem.Log(b)
--end


--return Main.Test()


function test( )
	-- body
	local target = Items.GetItem('01fda884-331d-4116-a84d-e1c9f8f6d03b')
	CoreSystem.Log(target.Name)

	target.OnUse = (function()

	end)

end

return test()