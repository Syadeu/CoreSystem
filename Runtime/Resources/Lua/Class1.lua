function Test()
	-- body
	local vec1 = Vector.ToVector3(0, 0, 0)
	local vec2 = Vector.ToVector3(10,10,10)

	local value = Vector.Lerp(vec1, vec2, 1)
	
	for i=1,3 do
		CoreSystem.Log(value[i])
	end

	return value
end

return Test()