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
	local target = Items.GetItem('1f56c346-e80d-4d65-9137-a1f63454d3f3')
	CoreSystem.Log(target.Name)

	target.SetValue('TestValue', 'teststring')

	target.OnUse = (function()
	end)

	Creature.OnVisible = (
		function(targetCreature)
			-- body
			CoreSystem.Log(targetCreature.Name .. " is displaying")
		end

	)
end

return test()